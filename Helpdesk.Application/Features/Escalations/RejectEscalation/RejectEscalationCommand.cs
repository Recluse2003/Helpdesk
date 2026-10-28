using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Entities;
using MediatR;

namespace Helpdesk.Application.Features.Escalations.RejectEscalation
{
    /// <summary>
    /// Represents a request to reject an active <see cref="EscalationRequest"/>. 
    /// </summary>
    /// <param name="UserId">Unique identifier of the user rejecting the <see cref="EscalationRequest"/>.</param>
    /// <param name="EscalationRequestId">Unique identifier of the <see cref="EscalationRequest"/> being rejected.</param>
    /// <param name="ResponseReason">May contain the reason why the request was rejected.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record RejectEscalationCommand(Guid UserId, Guid EscalationRequestId, string? ResponseReason) : IRequest<Result>;
}
