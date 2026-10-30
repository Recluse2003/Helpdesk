using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.AssignTicket
{
    /// <summary>
    /// Represents a request to assign a ticket to a support agent.
    /// </summary>
    /// <param name="TicketId">The unique identifier of the ticket to assign.</param>
    /// <param name="AssigneeId">The unique identifier of the support agent to assign the ticket to.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record AssignTicketCommand(Guid TicketId, Guid AssigneeId) : IRequest<Result>;
}
