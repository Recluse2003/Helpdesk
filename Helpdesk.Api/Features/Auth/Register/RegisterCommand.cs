using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.Register
{
    /// <summary>
    /// Represents a request to register a new account. 
    /// </summary>
    /// <param name="DisplayName">The display name of the new account.</param>
    /// <param name="Email">The email address of the new account.</param>
    /// <param name="Password">The password of the new account.</param>
    public record RegisterCommand(string DisplayName, string Email, string Password) : IRequest<Result>;
}
