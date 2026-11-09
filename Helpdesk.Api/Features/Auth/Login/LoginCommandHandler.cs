using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Helpdesk.Api.Features.Auth.Login.LoginEndpoint;

namespace Helpdesk.Api.Features.Auth.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly HelpdeskDbContext _context;

        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            ITokenService tokenService, 
            HelpdeskDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _context = context;
        }

        public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
                return Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");

            if (!user.EmailConfirmed)
                return Error.Conflict("Auth.EmailNotConfirmed", "Account email has not been confirmed.");

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (!signInResult.Succeeded)
                return Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.DisplayName, roles);

            var refreshToken = _tokenService.GenerateRefreshToken();

            var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);

            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            var refreshTokenEntity = new RefreshToken(
                user.Id,
                refreshTokenHash,
                refreshTokenExpiry);

            _context.RefreshTokens.Add(refreshTokenEntity);

            await _context.SaveChangesAsync(cancellationToken);

            return new LoginResponseDto(accessToken, refreshToken);
        }
    }
}
