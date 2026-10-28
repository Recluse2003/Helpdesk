using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Entities;
using MediatR;

namespace Helpdesk.Application.Features.Administrator.GetTickets
{
    /// <summary>
    /// Represents a request to retrieve all <see cref="Ticket"/>s for an admin. 
    /// </summary>
    /// <param name="UserId">Unique identifier of the user attempting to retrieve tickets. Only an admin may do this request.</param>
    /// <param name="PageNumber">The page number to retrieve.</param>
    /// <param name="PageSize">The number of entries to include per page.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a paginated list of <see cref="TicketDto"/> on success,
    /// or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetTicketsQuery(Guid UserId, int PageNumber, int PageSize) : IRequest<Result<PagedList<TicketDto>>>;
}
