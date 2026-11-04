using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Tickets.GetSupportQueue
{
    /// <summary>
    /// Represents a ticket displayed in the support queue, including its customer and assigned support agent information.
    /// </summary>
    /// <param name="Id">The unique identifier of the <see cref="Ticket"/>.</param>
    /// <param name="Title">The title of the <see cref="Ticket"/>.</param>
    /// <param name="Status">The current status of the <see cref="Ticket"/>.</param>
    /// <param name="Priority">The priority level of the <see cref="Ticket"/>.</param>
    /// <param name="CreatedAt">The date and time the <see cref="Ticket"/> was created.</param>
    /// <param name="UpdatedAt">The date and time the <see cref="Ticket"/> was last updated.</param>
    /// <param name="CreatedById">The unique identifier of the customer who created the <see cref="Ticket"/>.</param>
    /// <param name="CreatedByDisplayName">The display name of the customer who created the <see cref="Ticket"/>.</param>
    /// <param name="AssignedToId">The unique identifier of the assigned support agent, or null if the <see cref="Ticket"/> is unassigned.</param>
    /// <param name="AssigneeDisplayName">The display name of the assigned support agent, or null if the <see cref="Ticket"/> is unassigned.</param>
    public record QueueTicketDto(
        Guid Id,
        string Title,
        TicketStatus Status,
        TicketPriority Priority,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        Guid CreatedById,
        string CreatedByDisplayName,
        Guid? AssignedToId,
        string? AssigneeDisplayName
    );
}