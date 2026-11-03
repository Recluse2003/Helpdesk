using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.GetSupportQueue;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Escalations.GetEscalations
{
    public class GetEscalationsQueryHandler : IRequestHandler<GetEscalationsQuery, Result<PagedList<EscalationRequestDto>>>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetEscalationsQueryHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Result<PagedList<EscalationRequestDto>>> Handle(GetEscalationsQuery query, CancellationToken cancellationToken)
        {
            int pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            int pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var baseQuery = _context.EscalationRequests
                .AsNoTracking()
                .AsQueryable();

            if (query.Status.HasValue)
                baseQuery = baseQuery.Where(er => er.Status == query.Status.Value);

            if (query.TicketId.HasValue)
                baseQuery = baseQuery.Where(er => er.TicketId == query.TicketId.Value);

            if (query.RequestedByUserId.HasValue)
                baseQuery = baseQuery.Where(er => er.RequestedByUserId == query.RequestedByUserId.Value);

            int total = await baseQuery.CountAsync(cancellationToken);

            var requests = await (
                from er in baseQuery

                join ticket in _context.Tickets
                    on er.TicketId equals ticket.Id

                join requester in _context.Users
                    on er.RequestedByUserId equals requester.Id

                join recipient in _context.Users
                    on er.ProposedRecipientUserId equals (Guid?)recipient.Id
                    into recipientGroup

                from recipient in recipientGroup.DefaultIfEmpty()

                orderby er.Status == EscalationRequestStatus.Waiting descending,
                        er.CreatedAt descending

                select new EscalationRequestDto(
                    er.Id,
                    er.TicketId,
                    ticket.Title,
                    er.RequestedByUserId,
                    requester.DisplayName,
                    er.ProposedRecipientUserId,
                    recipient == null ? null : recipient.DisplayName,
                    er.Status,
                    er.CreatedAt,
                    er.RespondedAt
                )
            )
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

            PagedList<EscalationRequestDto> result = new(requests, total, pageNumber, pageSize);

            return result;
        }
    }
}
