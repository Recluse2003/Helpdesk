using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.ForgotPassword
{
    public record ForgotPasswordCommand(string Email) : IRequest<Result>;
}
