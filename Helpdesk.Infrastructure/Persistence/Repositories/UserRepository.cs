using Helpdesk.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly HelpdeskDbContext _context;

        public UserRepository(HelpdeskDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return _context.Users.AnyAsync(user => user.Id == userId, cancellationToken);
        }
    }
}
