using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.ResetPassword
{
    public record ResetPasswordCommand(Guid UserId, string Code, string NewPassword) : IRequest<Result>;
}
