using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;
}
