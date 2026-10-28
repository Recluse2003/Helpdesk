using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Entities;
using MediatR;

namespace Helpdesk.Application.Features.Tickets.AddTicketMessage
{
    /// <summary>
    /// Represents a request to add a message or internal note to a <see cref="Ticket"/>.
    /// </summary>
    /// <param name="TicketId">The unique identifier of the <see cref="Ticket"/> to add the message to.</param>
    /// <param name="AuthorUserId">The unique identifier of the user creating the new message.</param>
    /// <param name="Content">The text content of the message.</param>
    /// <param name="IsPublic">Indicates whether the message is visible to the customer.</param>
    /// <returns>
    /// A <see cref="Result"/> on success, or a <see cref="Result"/> containing an <see cref="Error"/> describing why the 
    /// operation failed.
    /// </returns>
    public record AddTicketMessageCommand(Guid TicketId, Guid AuthorUserId, string Content, bool IsPublic) : IRequest<Result>;
}
