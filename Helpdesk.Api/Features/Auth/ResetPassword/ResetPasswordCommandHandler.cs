using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Helpdesk.Api.Features.Auth.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("Auth.UserNotFound", "An account with this ID does not exist.");

            if (!user.EmailConfirmed)
                return Error.Conflict("Auth.EmailNotConfirmed", "Account email has not been confirmed.");

            string code;

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(command.Code));
            }
            catch (FormatException)
            {
                return Error.Conflict("Auth.PasswordResetFailure", "The password reset code is invalid or malformed.");
            }

            var result = await _userManager.ResetPasswordAsync(user, code, command.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors;

                if (errors.Any(e => e.Code.StartsWith("Password")))
                    return Error.Validation("Auth.InvalidPassword", "The new password does not meet the password requirements.");

                return Error.Conflict("Auth.PasswordResetFailure", "The password reset code is invalid or has expired.");
            }

            return Result.Success();
        }
    }
}
