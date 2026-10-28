using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Entities;
using MediatR;

namespace Helpdesk.Application.Features.Escalations.AcceptEscalation
{
    /// <summary>
    /// Represents a request to accept an active <see cref="EscalationRequest"/>. 
    /// </summary>
    /// <param name="UserId">Unique identifier of the user accepting the <see cref="EscalationRequest"/>.</param>
    /// <param name="EscalationRequestId">Unique identifier of the <see cref="EscalationRequest"/> being accepted.</param>
    /// <param name="ResponseReason">May contain the reason why the request was accepted.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record AcceptEscalationCommand(Guid UserId, Guid EscalationRequestId, string? ResponseReason) : IRequest<Result>;
}
