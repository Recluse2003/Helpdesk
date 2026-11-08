using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.ConfirmEmail
{
    /// <summary>
    /// Represents a request by a user to confirm their email address for a newly created account. 
    /// </summary>
    /// <param name="UserId">The unique identifier of the user's new account to be confirmed.</param>
    /// <param name="Code">The code provided to the confirm the user's new account.</param>
    public record ConfirmEmailCommand(Guid UserId, string Code) : IRequest<Result>;
}
