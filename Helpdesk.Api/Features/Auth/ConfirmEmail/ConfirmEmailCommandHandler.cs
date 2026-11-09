using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Helpdesk.Api.Features.Auth.ConfirmEmail
{
    /// <summary>
    /// Handles incoming confirm email commands. 
    /// </summary>
    /// <remarks>
    /// The handler verifies that the user exists, that their email address has not already been confirmed, and that the 
    /// supplied confirmation is valid and has not expired.
    /// </remarks>
    public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfirmEmailCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Processes an email confirmation request. 
        /// </summary>
        /// <param name="command">Contains the ID of the account being verified, as well as the email confirmation code.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> explaining
        /// why the request failed. 
        /// </returns>
        public async Task<Result> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("Auth.UserNotFound", "An account with this ID does not exist.");

            if (user.EmailConfirmed)
                return Error.Conflict("Auth.EmailAlreadyConfirmed", "This email address has already been confirmed.");

            // Decodes the supplied email confirmation code, and attempts to confirm the email. 
            string code;

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(command.Code));
            }
            catch (FormatException)
            {
                return Error.Conflict("Auth.EmailConfirmationFailure", "The confirmation code is invalid or malformed.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (!result.Succeeded)
                return Error.Conflict("Auth.EmailConfirmationFailure", "The confirmation code is invalid or has expired.");

            return Result.Success();
        }
    }
}
