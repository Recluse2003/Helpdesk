using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Application.Features.Tickets.GetSupportQueue;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Tickets.GetSupportQueue
{
    public class GetSupportQueueQueryHandler : IRequestHandler<GetSupportQueueQuery, Result<PagedList<QueueTicketDto>>>
    {
        private readonly HelpdeskDbContext _context;

        public GetSupportQueueQueryHandler(HelpdeskDbContext context)
        {
            _context = context;
        }

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
                    user => user.Id,
                    (x, users) => new { x.ticket, x.customer, users }
                )
                .SelectMany(
                    x => x.users.DefaultIfEmpty(),
                    (x, assignee) => new QueueTicketDto(
                        x.ticket.Id,
                        x.ticket.Title,
                        x.ticket.Status,
                        x.ticket.Priority,
                        x.ticket.CreatedAt,
                        x.ticket.UpdatedAt,
                        x.ticket.CreatedById,
                        x.customer.DisplayName,
                        x.ticket.AssignedToId,
                        assignee != null ? assignee.DisplayName : null
                    )
                )
                .OrderByDescending(dto => dto.UpdatedAt)
                .ThenBy(dto => dto.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            PagedList<QueueTicketDto> pagedList = new(tickets, pageNumber, pageSize, total);

            return pagedList;
        }
    }
}
