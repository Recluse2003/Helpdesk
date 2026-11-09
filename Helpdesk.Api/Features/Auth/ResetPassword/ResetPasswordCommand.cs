using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.ResetPassword
{
    /// <summary>
    /// Represents a request to reset the password of an existing user account using a valid password reset code.
    /// </summary>
    /// <param name="UserId">Unique identifier of the account whose password is being reset. </param>
    /// <param name="Code">The password reset verification code sent to the user's email address.</param>
    /// <param name="NewPassword">The new password to be assigned to the account.</param>
    public record ResetPasswordCommand(Guid UserId, string Code, string NewPassword) : IRequest<Result>;
}
