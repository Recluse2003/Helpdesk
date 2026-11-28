using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Administrator.GetUsers
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PagedList<UserDto>>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HelpdeskDbContext _context;

        public GetUsersQueryHandler(UserManager<ApplicationUser> userManager, HelpdeskDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<Result<PagedList<UserDto>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
        {
            // Guard to prevent page number from being lower than 1.
            int pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

            // Guard to prevent requests for less than 1 or more than 100 summaries. 
            int pageSize = query.PageSize < 1 ? 10 : Math.Min(query.PageSize, 100);

            var baseQuery = _context.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.Trim();

                baseQuery = baseQuery.Where(u =>
                    u.Id.ToString() == searchTerm ||
                    u.DisplayName.Contains(searchTerm) ||
                    (u.Email != null && u.Email.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(query.Role))
            {
                baseQuery = baseQuery.Where(u =>
                    _context.UserRoles.Any(ur =>
                        ur.UserId == u.Id &&
                        _context.Roles.Any(r =>
                            r.Id == ur.RoleId &&
                            r.Name == query.Role)));
            }

            if (query.EmailConfirmed.HasValue)
                baseQuery = baseQuery.Where(u => u.EmailConfirmed == query.EmailConfirmed.Value);

            int total = await baseQuery.CountAsync(cancellationToken);

            var users = await baseQuery
                .OrderBy(u => u.DisplayName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var userIds = users.Select(u => u.Id).ToList();

            var userRoles = await (
                from userRole in _context.UserRoles
                join role in _context.Roles
                    on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new
                {
                    userRole.UserId,
                    RoleName = role.Name
                }
            ).ToListAsync(cancellationToken);

            var userDtos = users.Select(user => new UserDto(
                user.Id,
                user.DisplayName,
                user!.Email,
                userRoles.FirstOrDefault(r => r.UserId == user.Id)?.RoleName ?? "Unknown",
                user.EmailConfirmed
            )).ToList();

            return new PagedList<UserDto>(userDtos, query.PageNumber, query.PageSize, total);
        }
    }
}
