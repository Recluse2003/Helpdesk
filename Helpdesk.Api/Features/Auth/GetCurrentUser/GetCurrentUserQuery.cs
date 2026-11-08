using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.GetCurrentUser
{
    public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<GetCurrentUserResponse>>;
}
