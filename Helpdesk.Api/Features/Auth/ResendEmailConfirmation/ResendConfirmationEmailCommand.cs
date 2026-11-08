using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.ResendEmailConfirmation
{
    public record ResendConfirmationEmailCommand(string Email) : IRequest<Result>;
}
