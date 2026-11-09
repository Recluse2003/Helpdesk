using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.Logout
{
    /// <summary>
    /// Represents a request to log out a user by revoking their refresh token. 
    /// </summary>
    /// <param name="RefreshToken">The refresh token associated with the user's active session.</param>
    public record LogoutCommand(string RefreshToken) : IRequest<Result>;
}
