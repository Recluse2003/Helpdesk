using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Auth.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;

        public LogoutCommandHandler(
            HelpdeskDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService)
        {
            _context = dbContext;
            _userManager = userManager;
            _tokenService = tokenService;
        }

        public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var tokenHash = _tokenService.HashRefreshToken(command.RefreshToken);

            await _context.RefreshTokens
                .Where(x =>
                    x.TokenHash == tokenHash &&
                    x.RevokedAt == null &&
                    x.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.RevokedAt, now),
                    cancellationToken);

            return Result.Success();
        }
    }
}
