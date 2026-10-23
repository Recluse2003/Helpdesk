using Helpdesk.Application.Common.Results;
using Helpdesk.Domain.Enums;
using MediatR;

namespace Helpdesk.Application.Tickets.GetSupportQueue
{
    /// <summary>
    /// Represents a request to retrieve support queue tickets, optionally filtered by status, priority, and assignee.
    /// </summary>
    /// <param name="Status">The status to filter tickets by, or <see langword="null"/> to include tickets with any status.</param>
    /// <param name="Priority">The priority to filter tickets by, or <see langword="null"/> to include tickets with any priority.</param>
    /// <param name="AssigneeId">The unique identifier of the assigned support agent to filter by, or <see langword="null"/> to include tickets regardless of assignee.</param>
    public record GetSupportQueueQuery(TicketStatus? Status, TicketPriority? Priority, Guid? AssigneeId) 
        : IRequest<Result<IReadOnlyList<QueueTicketDto>>>;
}
