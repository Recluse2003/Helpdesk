using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.GetSupportQueue
{
    /// <summary>
    /// Represents a query for retrieving a paginated list of tickets in the support queue, with optional filtering, searching,
    /// and sorting.
    /// </summary>
    /// <param name="PageNumber">The page number of the support queue.</param>
    /// <param name="PageSize">The max number of queue tickets on the page.</param>
    /// <param name="Status">The status to filter tickets by, or <see langword="null"/> to include tickets with any status.</param>
    /// <param name="Priority">The priority to filter tickets by, or <see langword="null"/> to include tickets with any priority.</param>
    /// <param name="SearchTerm">An optional search term used to filter tickets by ticket title, ticket ID, customer ID or display name, 
    /// and assignee ID or display name.</param>
    /// <param name="AssigneeId">The unique identifier of the assigned support agent to filter ticket by, or <see langword="null"/> 
    /// to include tickets regardless of assignee.</param>
    /// <param name="UnassignedOnly">Indicates whether to include only tickets that have not been assigned to a support agent.</param>
    /// <param name="SortBy">The ticket property used to order the results. Defaults to <see cref="TicketSortBy.UpdatedAt"/>.</param>
    /// <param name="SortDescending">Indicates whether the results should be sorted in descending order. Defaults to 
    /// <see langword="true"/>.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a paginated list of <see cref="QueueTicketDto"/> on success,
    /// or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetSupportQueueQuery(
        int PageNumber = 1,
        int PageSize = 10,
        TicketStatus? Status = null,
        TicketPriority? Priority = null,
        string? SearchTerm = null,
        Guid? AssigneeId = null,
        bool UnassignedOnly = false,
        TicketSortBy SortBy = TicketSortBy.UpdatedAt,
        bool SortDescending = true
    ) : IRequest<Result<PagedList<QueueTicketDto>>>;
}
