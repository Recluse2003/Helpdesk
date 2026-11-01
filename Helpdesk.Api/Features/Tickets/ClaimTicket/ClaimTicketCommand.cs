using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.ClaimTicket
{
    /// <summary>
    /// Represents a request for a support agent to claim an unassigned <see cref="Ticket"/>.
    /// </summary>
    /// <param name="UserId">The unique identifier of the support agent claiming the <see cref="Ticket"/>.</param>
    /// <param name="TicketId">The unique identifier of the <see cref="Ticket"/> to claim.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record ClaimTicketCommand(Guid UserId, Guid TicketId) : IRequest<Result>;
}
