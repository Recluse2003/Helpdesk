using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.Login
{
    /// <summary>
    /// Represents a request to authenticate a user with their email address and password.
    /// </summary>
    /// <param name="Email">The email address associated with the user's account.</param>
    /// <param name="Password">The user's account password.</param>
    public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponseDto>>;
}
