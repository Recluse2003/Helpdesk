using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Entities;
using MediatR;

namespace Helpdesk.Application.Escalations.RequestEscalation
{
    /// <summary>
    /// Represents a request by a support agent to create an <see cref="EscalationRequest"/>, to escalate a <see cref="Ticket"/> 
    /// to a more qualified agent. 
    /// </summary>
    /// <param name="RequestedByUserId">Unique identifer of the support agent who requested escalation of a ticket. </param>
    /// <param name="ProposedRecipientUserId">Unique identifier of the support agent who the requester recommends to take over
    /// the <see cref="Ticket"/>.</param>
    /// <param name="Reason">The reason on why the support agent believes the escalation is required.</param>
    /// <param name="TicketId">Unique identifer of the <see cref="Ticket"/>, the to be created <see cref="EscalationRequest"/> is for.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record RequestEscalationCommand(Guid RequestedByUserId, Guid ProposedRecipientUserId, string Reason, Guid TicketId) : IRequest<Result>;
}
