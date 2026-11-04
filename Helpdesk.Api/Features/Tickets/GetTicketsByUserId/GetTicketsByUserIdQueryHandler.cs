using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Handles incoming queries for retrieving a paginated list of tickets created by a specified user.
    /// </summary>
    /// <remarks>
    /// Filters tickets by their creator, retrieves the assigned agent's display name when an agent is assigned, and 
    /// orders the results by their last updated date. Returns the matching tickets as a paginated list.
    /// </remarks>
    public class GetTicketsByUserIdQueryHandler : IRequestHandler<GetTicketsByUserIdQuery, Result<PagedList<TicketSummaryDto>>>
    {
        private readonly HelpdeskDbContext _context;

        public GetTicketsByUserIdQueryHandler(HelpdeskDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a paginated list of tickets created by the specified user.
        /// </summary>
        /// <param name="query">Contains the user identifier and pagination criteria.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous database operations.</param>
        /// <returns>
        /// A <see cref="Result"/> containing a paginated list of ticket summaries created by the specified user.
        /// </returns>
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
