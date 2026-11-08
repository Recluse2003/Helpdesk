using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Api.Features.Auth.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;

        public LoginCommandHandler(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
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

            var token = _tokenService.GenerateToken(user.Id, user.Email!, user.DisplayName, roles);

            return new LoginResponse(token.AccessToken, token.ExpiresAt);
        }
    }

    public record LoginResponse(string AccessToken, DateTime ExpiresAt);
}
