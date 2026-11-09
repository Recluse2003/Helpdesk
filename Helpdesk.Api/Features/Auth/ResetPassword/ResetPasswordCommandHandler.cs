using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Helpdesk.Api.Features.Auth.ResetPassword
{
    /// <summary>
    /// Handles incoming reset password requests.
    /// </summary>
    /// <remarks>
    /// The handler verifies that the account exists and has a confirmed email address before validating the password reset 
    /// code. Password updates are performed through ASP.NET Core Identity, which  validates the new password against the
    /// configured password requirements.
    /// </remarks>
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Processes a password reset request. 
        /// </summary>
        /// <param name="command">Contains the account's unique identifier, password reset code and new password.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> 
        /// explaining why the request failed. 
        /// </returns>
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
