using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.ResendEmailConfirmation
{
    /// <summary>
    /// Represents a request to resend a new account's email confirmation. 
    /// </summary>
    /// <param name="Email"></param>
    public record ResendConfirmationEmailCommand(string Email) : IRequest<Result>;
}
