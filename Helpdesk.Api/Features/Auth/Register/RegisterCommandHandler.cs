using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Api.Features.Auth.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email.Trim());

            if (existingUser != null)
                return Error.Conflict("Auth.EmailAlreadyRegistered", "An account with this email address already exists.");

            var user = new ApplicationUser
            {
                UserName = request.Email.Trim(),
                Email = request.Email.Trim(),
                DisplayName = request.DisplayName.Trim(),
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));

                return Error.Validation("Auth.RegistrationFailed", errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, RoleNames.Customer);

            if (!roleResult.Succeeded)
            {
                // Avoid leaving behind an account without its intended role.
                await _userManager.DeleteAsync(user);

                return Error.Failure("Auth.RoleAssignmentFailed", "Unable to assign the customer role.");
            }

            return Result.Success();
        }
    }
}
