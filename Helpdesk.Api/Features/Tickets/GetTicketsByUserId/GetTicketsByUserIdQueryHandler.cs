using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
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
    public class GetTicketsByUserIdQueryHandler : IRequestHandler<GetTicketsByUserIdQuery, Result<GetTicketsByUserIdResponse>>
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
        /// A <see cref="Result"/> containing a paginated list of ticket summaries created by the specified user, 
        /// as well as a statistics over all of the user's tickets.
        /// </returns>
        public async Task<Result<GetTicketsByUserIdResponse>> Handle(
            GetTicketsByUserIdQuery query,
            CancellationToken cancellationToken)
        {
            int pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            int pageSize = query.PageSize < 1 ? 10 : Math.Min(query.PageSize, 100);

            var baseQuery = _context.Tickets
                .AsNoTracking()
                .Where(t => t.CreatedById == query.UserId);

            // Retrieve statistics across all of the customer's tickets.
            var statisticsData = await baseQuery
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalTickets = g.Count(),
                    OpenTickets = g.Count(t => t.Status == TicketStatus.Open),
                    InProgressTickets = g.Count(t => t.Status == TicketStatus.Active),
                    AwaitingCustomerResponseTickets = g.Count(t => t.Status == TicketStatus.AwaitingCustomerResponse),
                    ResolvedTickets = g.Count(t => t.Status == TicketStatus.Resolved),
                    ClosedTickets = g.Count(t => t.Status == TicketStatus.Closed)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var statistics = new TicketStatisticsDto(
                statisticsData?.TotalTickets ?? 0,
                statisticsData?.OpenTickets ?? 0,
                statisticsData?.InProgressTickets ?? 0,
                statisticsData?.AwaitingCustomerResponseTickets ?? 0,
                statisticsData?.ResolvedTickets ?? 0,
                statisticsData?.ClosedTickets ?? 0
            );

            var ticketsQuery = baseQuery;

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                ticketsQuery = ticketsQuery.Where(t =>
                    t.Title.ToLower().Contains(query.SearchTerm.ToLower()) ||
                    t.Description.ToLower().Contains(query.SearchTerm.ToLower()));
            }

            if (query.Status.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t =>
                    t.Status == query.Status.Value);
            }

            var total = ticketsQuery.Count();

            var tickets = await ticketsQuery
                .GroupJoin(
                    _context.Users,
                    ticket => ticket.AssignedToId,
                    user => (Guid?)user.Id,
                    (ticket, users) => new { ticket, users }
                )
                .SelectMany(
                    x => x.users.DefaultIfEmpty(),
                    (x, user) => new
                    {
                        Ticket = x.ticket,
                        AssignedToName = user != null ? user.DisplayName : null
                    }
                )
                .OrderByDescending(x => x.Ticket.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new TicketSummaryDto(
                    x.Ticket.Id,
                    x.Ticket.Title,
                    x.Ticket.Status,
                    x.Ticket.Priority,
                    x.Ticket.CreatedAt,
                    x.Ticket.UpdatedAt,
                    x.Ticket.AssignedToId,
                    x.AssignedToName,
                    null
                ))
                .ToListAsync(cancellationToken);

            var pagedList = new PagedList<TicketSummaryDto>(
                tickets,
                pageNumber,
                pageSize,
                total
            );

            var response = new GetTicketsByUserIdResponse(
                pagedList,
                statistics
            );

            return response;
        }
    }
}
