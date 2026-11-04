using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Escalations.GetEscalations
{
    /// <summary>
    /// Handles incoming queries for retrieving a paginated collection of ticket escalation requests.
    /// </summary>
    /// <remarks>
    /// Supports filtering escalation requests by status, associated ticket and requesting user. Results are ordered
    /// with pending requests first, followed by the most recently created requests.
    /// </remarks>
    public class GetEscalationsQueryHandler : IRequestHandler<GetEscalationsQuery, Result<PagedList<EscalationRequestDto>>>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetEscalationsQueryHandler(HelpdeskDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Retrieves escalation requests matching the specified filters.
        /// </summary>
        /// <param name="query">Contains the optional filtering criteria and pagination settings.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> with a paginated collection of escalation request summaries.
        /// </returns>
        /// <remarks>
        /// Escalation requests are ordered with pending requests first, followed by requests ordered by creation 
        /// date in descending order. The query includes the associated ticket title, requester display name and proposed 
        /// recipient display name, where available.
        ///
        /// Invalid pagination values are replaced with default values. 
        /// </remarks>
        public async Task<Result<PagedList<EscalationRequestDto>>> Handle(GetEscalationsQuery query, CancellationToken cancellationToken)
        {
            // Guard to prevent page number from being lower than 1.
            int pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

            // Guard to prevent requests for less than 1 or more than 100 summaries. 
            int pageSize = query.PageSize < 1 ? 10 : Math.Min(query.PageSize, 100);

            var baseQuery = _context.EscalationRequests
                .AsNoTracking()
                .AsQueryable();

            // If provided, only retrieves requests with this current status
            if (query.Status.HasValue)
                baseQuery = baseQuery.Where(er => er.Status == query.Status.Value);

            // If provided, only retrieves requests associated with this ticket ID
            if (query.TicketId.HasValue)
                baseQuery = baseQuery.Where(er => er.TicketId == query.TicketId.Value);

            // If provided, only retrieves escalations submitted by the provided user ID
            if (query.RequestedByUserId.HasValue)
                baseQuery = baseQuery.Where(er => er.RequestedByUserId == query.RequestedByUserId.Value);

            int total = await baseQuery.CountAsync(cancellationToken);

            // Constructs the requested escalation request summaries
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
