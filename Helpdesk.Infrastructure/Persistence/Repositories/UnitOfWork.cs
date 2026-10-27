using Helpdesk.Application.Common.Interfaces;

namespace Helpdesk.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HelpdeskDbContext _context;

        public UnitOfWork(HelpdeskDbContext context)
        {
            _context = context;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
