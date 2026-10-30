using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Handles the execution of a user's <see cref="GetTicketsByUserIdQuery"/> request.
    /// </summary>
    public class GetTicketsByUserIdQueryHandler : IRequestHandler<GetTicketsByUserIdQuery, Result<PagedList<TicketSummaryDto>>>
    {
        private readonly HelpdeskDbContext _context;

        public GetTicketsByUserIdQueryHandler(HelpdeskDbContext context)
        {
            _context = context;
        }

        public async Task<Result<PagedList<TicketSummaryDto>>> Handle(GetTicketsByUserIdQuery query, CancellationToken cancellationToken)
        {
            int pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            int pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var baseQuery = _context.Tickets
                .AsNoTracking()
                .Where(t => t.CreatedById == query.UserId);

            int total = await baseQuery.CountAsync(cancellationToken);

            var tickets = await baseQuery
                .GroupJoin(
                    _context.Users,
                    ticket => ticket.AssignedToId,
                    user => user.Id,
                    (ticket, users) => new { ticket, users }
                )
                .SelectMany(
                    x => x.users.DefaultIfEmpty(),
                    (x, user) => new TicketSummaryDto(
                        x.ticket.Id,
                        x.ticket.Title,
                        x.ticket.Status,
                        x.ticket.Priority,
                        x.ticket.CreatedAt,
                        x.ticket.UpdatedAt,
                        user != null ? user.DisplayName : null
                    )
                )
                .OrderByDescending(dto => dto.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            PagedList<TicketSummaryDto> pagedList = new (tickets, pageNumber, pageSize, total);

            return pagedList;
        }
    }
}
