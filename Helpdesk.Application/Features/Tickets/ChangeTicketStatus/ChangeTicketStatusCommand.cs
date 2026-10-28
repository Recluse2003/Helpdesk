using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Entities;
using Helpdesk.Domain.Enums;
using MediatR;

namespace Helpdesk.Application.Features.Tickets.ChangeTicketStatus
{
    /// <summary>
    /// Represents a request to change the status on a specified <see cref="Ticket"/>. Only the customer, or assigned agent may 
    /// change a <see cref="Ticket"/>'s status. 
    /// </summary>
    /// <param name="UserId">Unique identifier of user attempting to change the specified <see cref="Ticket"/> status.</param>
    /// <param name="TicketId">Unique identifier of the <see cref="Ticket"/> who's status is being changed.</param>
    /// <param name="NewStatus">The new status the ticket is being assigned.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record ChangeTicketStatusCommand(Guid UserId, Guid TicketId, TicketStatus NewStatus) : IRequest<Result>;
}
