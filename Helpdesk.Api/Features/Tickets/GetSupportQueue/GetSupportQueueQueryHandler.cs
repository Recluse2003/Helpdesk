using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Tickets.GetSupportQueue
{
    /// <summary>
    /// Handles incoming queries for retrieving a paginated list of tickets in the support queue.
    /// </summary>
    /// <remarks>
    /// Applies the requested status, priority, customer and assignee filters. 
    /// </remarks>
    public class GetSupportQueueQueryHandler : IRequestHandler<GetSupportQueueQuery, Result<PagedList<QueueTicketDto>>>
    {
        private readonly HelpdeskDbContext _context;

        public GetSupportQueueQueryHandler(HelpdeskDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a filtered and paginated list of ticket summaries for the support queue.
        /// </summary>
        /// <param name="query">The filtering and pagination criteria.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous database operations.</param>
        /// <returns>
        /// A <see cref="Result"/> containing a <see cref="PagedList{QueueTicketDto}"/> containing a collection of
        /// <see cref="QueueTicketDto"/>.
        /// </returns>
        public async Task<Result<PagedList<QueueTicketDto>>> Handle(GetSupportQueueQuery query, CancellationToken cancellationToken)
        {
            int pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            int pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var baseQuery = _context.Tickets
                .AsNoTracking()
                .AsQueryable();

            if (query.Status.HasValue)
                baseQuery = baseQuery.Where(t => t.Status == query.Status.Value);

            if (query.Priority.HasValue)
                baseQuery = baseQuery.Where(t => t.Priority == query.Priority.Value);

            if (query.CustomerId.HasValue)
                baseQuery = baseQuery.Where(t => t.CreatedById == query.CustomerId.Value);

            if (query.AssigneeId.HasValue)
                baseQuery = baseQuery.Where(t => t.AssignedToId == query.AssigneeId.Value);

            int total = await baseQuery.CountAsync(cancellationToken);

            var tickets = await baseQuery
                .Join(
                    _context.Users,
                    ticket => ticket.CreatedById,
                    user => user.Id,
                    (ticket, customer) => new { ticket, customer }
                )
                .GroupJoin(
                    _context.Users,
                    x => x.ticket.AssignedToId,
                    user => (Guid?)user.Id,
                    (x, users) => new { x.ticket, x.customer, users }
                )
                .SelectMany(
                    x => x.users.DefaultIfEmpty(),
                    (x, assignee) => new
                    {
                        Ticket = x.ticket,
                        CustomerDisplayName = x.customer.DisplayName,
                        AssigneeDisplayName = assignee == null
                            ? null
                            : assignee.DisplayName
                    }
                )
                // Order using entity properties before constructing the DTO.
                .OrderByDescending(x => x.Ticket.UpdatedAt)
                .ThenBy(x => x.Ticket.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new QueueTicketDto(
                    x.Ticket.Id,
                    x.Ticket.Title,
                    x.Ticket.Status,
                    x.Ticket.Priority,
                    x.Ticket.CreatedAt,
                    x.Ticket.UpdatedAt,
                    x.Ticket.CreatedById,
                    x.CustomerDisplayName,
                    x.Ticket.AssignedToId,
                    x.AssigneeDisplayName
                ))
                .ToListAsync(cancellationToken);

            PagedList<QueueTicketDto> result = new(tickets, pageNumber, pageSize, total);

            return result;
        }
    }
}
