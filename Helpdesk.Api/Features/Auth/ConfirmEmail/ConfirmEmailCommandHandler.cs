using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Helpdesk.Api.Features.Auth.ConfirmEmail
{
    public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfirmEmailCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());

            if (user == null)
                return Error.NotFound("Auth.UserNotFound", "An account with this ID does not exist.");

            if (user.EmailConfirmed)
                return Error.Conflict("Auth.EmailAlreadyConfirmed", "This email address has already been confirmed.");

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
