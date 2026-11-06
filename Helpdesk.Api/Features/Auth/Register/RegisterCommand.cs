using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.Register
{
    public record RegisterCommand(string DisplayName, string Email, string Password) : IRequest<Result>;
}
