using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.Logout
{
    public record LogoutCommand(string RefreshToken) : IRequest<Result>;
}
