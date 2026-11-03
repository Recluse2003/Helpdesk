using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using MediatR;

namespace Helpdesk.Api.Features.Escalations.GetEscalations
{
    /// <summary>
    /// Represents a request to retrieve <see cref="EscalationRequest"/>s, optionally filtered by status, ticket ID, and 
    /// requested by user ID.
    /// </summary>
    /// <param name="PageNumber">The page number of the support queue.</param>
    /// <param name="PageSize">The max number of queue tickets on the page.</param>
    /// <param name="RequestStatus">The status to filter the <see cref="EscalationRequest"/>s by,or <see langword="null"/> 
    /// to include <see cref="EscalationRequest"/> with any status. </param>
    /// <param name="TicketId">The unique identifer of the <see cref="Ticket"/> that the <see cref="EscalationRequest"/>s 
    /// are for to filter by, or <see langword="null"/> to include <see cref="EscalationRequest"/>s regardless of which 
    /// <see cref="Ticket"/> it is for.</param>
    /// <param name="RequestedByUserId">The unique identifer of the support agent who requested <see cref="EscalationRequest"/>s 
    /// to filter by, or <see langword="null"/> to include <see cref="EscalationRequest"/>s regardless of who requested them.
    /// </param>
    /// <returns>
    /// A <see cref="Result"/> containing a paginated list of <see cref="EscalationRequestDto"/> on success,
    /// or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetEscalationsQuery(
        int PageNumber = 1, 
        int PageSize = 10, 
        EscalationRequestStatus? RequestStatus = null, 
        Guid? TicketId = null, 
        Guid? RequestedByUserId = null
    ) : IRequest<Result<PagedList<EscalationRequestDto>>>;
}
