using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Features.Auth.Refresh
{
    public class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<RefreshResponseDto>>
    {
        private readonly HelpdeskDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;

        public RefreshCommandHandler(
            HelpdeskDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService)
        {
            _context = dbContext;
            _userManager = userManager;
            _tokenService = tokenService;
        }

        public async Task<Result<RefreshResponseDto>> Handle(RefreshCommand command, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var tokenHash = _tokenService.HashRefreshToken(
                command.RefreshToken);

            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (storedToken == null || !storedToken.IsActive)
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token.");

            var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());

            if (user == null || !user.EmailConfirmed)
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token.");

            if (await _userManager.IsLockedOutAsync(user))
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token.");
            

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = _tokenService.GenerateAccessToken(
                user.Id,
                user.Email!,
                user.DisplayName,
                roles);

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var newRefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);

            var newRefreshTokenExpiry = now.AddDays(7);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Revoke the old token only if it is still active.
            var updated = await _context.RefreshTokens
                .Where(x =>
                    x.Id == storedToken.Id &&
                    x.RevokedAt == null &&
                    x.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.RevokedAt, now)
                        .SetProperty(
                            x => x.ReplacedByTokenHash,
                            newRefreshTokenHash),
                    cancellationToken);

            if (updated == 0)
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token.");

            var replacement = new RefreshToken(
                user.Id,
                newRefreshTokenHash,
                newRefreshTokenExpiry);

            _context.RefreshTokens.Add(replacement);

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new RefreshResponseDto(accessToken, newRefreshToken);
        }
    }
}
