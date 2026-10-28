using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Entities;
using MediatR;

namespace Helpdesk.Application.Features.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Represents a request to retrieve a paginated list of <see cref="Ticket"/>s associated with a specified user.
    /// </summary>
    /// <param name="UserId">The unique identifier of the user whose tickets are to be retrieved.</param>
    /// <param name="PageNumber">The page number to retrieve.</param>
    /// <param name="PageSize">The number of tickets to include per page.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a paginated list of <see cref="TicketSummaryDto"/> on success,
    /// or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetTicketsByUserIdQuery(Guid UserId, int PageNumber, int PageSize) : IRequest<Result<PagedList<TicketSummaryDto>>>;
}
