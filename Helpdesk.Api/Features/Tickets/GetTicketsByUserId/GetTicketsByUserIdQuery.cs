using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Represents a query for retrieving a paginated collection of tickets created by a specified user, with optional 
    /// search and status filtering.
    /// </summary>
    /// <param name="UserId">The unique identifier of the user whose tickets are to be retrieved.</param>
    /// <param name="PageNumber">The page number to retrieve.</param>
    /// <param name="PageSize">The number of tickets to include per page.</param>
    /// <param name="SearchTerm">An optional search term used to filter tickets by their title or description. When null or 
    /// empty, no search term filtering is applied to the result.</param>
    /// <param name="Status">An option status filter used to filter tickets. When no status filter is supplied, all tickets
    /// statuses will be included.</param>
    /// <returns>
    /// A <see cref="Result{TValue}"/> containing a <see cref="GetTicketsByUserIdResponse"/> with the paginated ticket
    /// summaries and aggregate ticket statistics, or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetTicketsByUserIdQuery(Guid UserId, int PageNumber, int PageSize, string? SearchTerm = null, TicketStatus? Status = null) : IRequest<Result<GetTicketsByUserIdResponse>>;
}
