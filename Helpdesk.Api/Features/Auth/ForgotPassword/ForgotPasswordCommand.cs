using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.ForgotPassword
{
    /// <summary>
    /// Represents a request to initiate the password reset process for a user account. 
    /// </summary>
    /// <param name="Email">The email address associated with the account requesting a password reset.</param>
    public record ForgotPasswordCommand(string Email) : IRequest<Result>;
}
