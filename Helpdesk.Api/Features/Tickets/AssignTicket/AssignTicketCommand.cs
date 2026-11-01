using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.AssignTicket
{
    /// <summary>
    /// Represents a request for an administrator to assign or reassign a <see cref="Ticket"/>.
    /// </summary>
    /// <param name="UserId">The unique identifier of the administrator performing the assignment.</param>
    /// <param name="TicketId">The unique identifier of the <see cref="Ticket"/> to assign.</param>
    /// <param name="AssigneeId">The unique identifier of the support agent receiving the <see cref="Ticket"/>.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record AssignTicketCommand(Guid UserId, Guid TicketId, Guid AssigneeId) : IRequest<Result>;
}
