using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.GetCurrentUser
{
    /// <summary>
    /// Represents a request to retrieve the details of the current user. 
    /// </summary>
    /// <param name="UserId">The unique identifier of the current user.</param>
    public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<GetCurrentUserResponse>>;
}
