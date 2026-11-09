using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Auth.Logout
{
    /// <summary>
    /// Handles incoming logout command. 
    /// </summary>
    /// <remarks>
    /// Revokes the provided refresh token. 
    /// </remarks>
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

        /// <summary>
        /// Processes an account logout command.
        /// </summary>
        /// <param name="command">Contains the refresh token of the active session.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/>
        /// explaining why the request failed. </returns>
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
