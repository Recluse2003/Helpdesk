using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.GetSupportQueue;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.GetSupportQueue
{
    /// <summary>
    /// Represents a request to retrieve support queue tickets, optionally filtered by status, priority, and assignee.
    /// </summary>
    /// <param name="PageNumber">The page number of the support queue.</param>
    /// <param name="PageSize">The max number of queue tickets on the page.</param>
    /// <param name="Status">The status to filter tickets by, or <see langword="null"/> to include tickets with any status.</param>
    /// <param name="Priority">The priority to filter tickets by, or <see langword="null"/> to include tickets with any priority.</param>
    /// <param name="CustomerId">The unique identifier of the customer to filter tickets by, or <see langword="null"/> to include tickets regardless of customer.</param>
    /// <param name="AssigneeId">The unique identifier of the assigned support agent to filter ticket by, or <see langword="null"/> to include tickets regardless of assignee.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a paginated list of <see cref="QueueTicketDto"/> on success,
    /// or an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetSupportQueueQuery(int PageNumber = 1, int PageSize = 10, TicketStatus? Status = null, TicketPriority? Priority = null, Guid? CustomerId = null, Guid? AssigneeId = null) 
        : IRequest<Result<PagedList<QueueTicketDto>>>;
}
