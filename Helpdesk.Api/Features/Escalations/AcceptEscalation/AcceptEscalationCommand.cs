using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using MediatR;

namespace Helpdesk.Api.Features.Escalations.AcceptEscalation
{
    /// <summary>
    /// Represents a request to accept an active <see cref="EscalationRequest"/>. 
    /// </summary>
    /// <param name="UserId">Unique identifier of the user accepting the <see cref="EscalationRequest"/>.</param>
    /// <param name="EscalationRequestId">Unique identifier of the <see cref="EscalationRequest"/> being accepted.</param>
    /// <param name="NewSupportAgentId">Unique identifier of the support agent to assign the ticket to.</param>
    /// <param name="ResponseReason">Optional reason for why the request was accepted.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record AcceptEscalationCommand(Guid UserId, Guid EscalationRequestId, Guid NewSupportAgentId, string? ResponseReason) : IRequest<Result>;
}
