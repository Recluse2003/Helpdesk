using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.ChangeTicketPriority
{
    /// <summary>
    /// Represents a request to change the priority on a specified <see cref="Ticket"/>. Only an assigned agent may 
    /// change a <see cref="Ticket"/>'s priority after it's creation. 
    /// </summary>
    /// <param name="UserId">Unique identifier of support agent attempting to change the specified <see cref="Ticket"/> 
    /// status.</param>
    /// <param name="TicketId">Unique identifier of the <see cref="Ticket"/> who's priority is being changed.</param>
    /// <param name="NewPriority">The new priority the <see cref="Ticket"/> is being assigned.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record ChangeTicketPriorityCommand(Guid UserId, Guid TicketId, TicketPriority NewPriority) : IRequest<Result>;
}
