using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.Refresh
{
    /// <summary>
    /// Represents a request to obtain new access and refresh tokens by providing the refresh token of the current active 
    /// session.
    /// </summary>
    /// <param name="RefreshToken">The refresh token associated with the user's active session.</param>
    public record RefreshCommand(string RefreshToken) : IRequest<Result<RefreshResponseDto>>;
}
