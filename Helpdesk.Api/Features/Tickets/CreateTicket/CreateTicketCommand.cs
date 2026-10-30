using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Entities;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.CreateTicket
{
    /// <summary>
    /// Represents a request to create a new support ticket.
    /// </summary>
    /// <param name="UserId">Unique identifer of the user creating the ticket.</param>
    /// <param name="Title">The title stating the issue being reported.</param>
    /// <param name="Description">A detailed description of the issue.</param>
    /// <param name="Priority">The customer requested priority of the ticket.</param>
    /// <returns>
    /// A <see cref="Result"/> containing the unique identifier of the newly created <see cref="Ticket"/> on success,
    /// or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record CreateTicketCommand(Guid UserId, string Title, string Description, TicketPriority Priority) : IRequest<Result<Guid>>;
}
