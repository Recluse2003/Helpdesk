using Helpdesk.Application.Common.Interfaces;
using Helpdesk.Application.Common.Results;
using Helpdesk.Application.Features.Tickets.GetTicketsByUserId;
using Helpdesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Infrastructure.Persistence.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly HelpdeskDbContext _context;

        public TicketRepository(HelpdeskDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
        {
            await _context.AddAsync(ticket, cancellationToken);
        }

        public async Task<PagedList<TicketSummaryDto>> GetTicketsByUserId(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var query = _context.Tickets
                .AsNoTracking()
                .Where(t => t.CreatedById == userId);

            int total = query.Count();

            IReadOnlyCollection<TicketSummaryDto> tickets = await query
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
                        user != null ? user.UserName : null
                    )
                )
                .OrderByDescending(dto => dto.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedList<TicketSummaryDto>(tickets, pageNumber, pageSize, total);
        }
    }
}
