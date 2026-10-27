using Helpdesk.Application.Common.Interfaces;
using Helpdesk.Domain.Entities;

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
    }
}
