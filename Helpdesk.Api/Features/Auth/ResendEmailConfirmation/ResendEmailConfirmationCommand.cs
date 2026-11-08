using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.ResendEmailConfirmation
{
    public record ResendEmailConfirmationCommand(string Email) : IRequest<Result>;
}
