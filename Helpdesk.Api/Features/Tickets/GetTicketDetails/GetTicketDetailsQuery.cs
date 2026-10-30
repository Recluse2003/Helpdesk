using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.GetTicketDetails
{
    /// <summary>
    /// Represents a request to retrieve the full details of a specified <see cref="Ticket"/>, including
    /// <see cref="TicketMessage"/>s and <see cref="TicketActivity"/>s associated with it. Only retrieves public messages for 
    /// customer, but retrieves private notes and activities for support agents. 
    /// </summary>
    /// <param name="UserId">Unique identifier of user requesting details of specified <see cref="Ticket"/>.</param>
    /// <param name="TicketId">Unique identifier of ticket being retrieved.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a <see cref="TicketDetailsDto"/> which contains all related data to the
    /// <see cref="Ticket"/>on success, or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetTicketDetailsQuery(Guid UserId, Guid TicketId) : IRequest<Result<TicketDetailsDto>>;
}
