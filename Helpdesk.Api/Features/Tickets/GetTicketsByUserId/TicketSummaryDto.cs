using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Represents a summary of a <see cref="Ticket"/>, including it's current status, priority and assigned support agent.
    /// </summary>
    /// <param name="Id">The unique identifier of the <see cref="Ticket"/>.</param>
    /// <param name="Title">The title of the <see cref="Ticket"/>.</param>
    /// <param name="Status">The current status of the <see cref="Ticket"/>.</param>
    /// <param name="Priority">The priority of the <see cref="Ticket"/>.</param>
    /// <param name="CreatedAt">The date and time the <see cref="Ticket"/> was created.</param>
    /// <param name="UpdatedAt">The date and time the <see cref="Ticket"/> was last updated.</param>
    /// <param name="AssignedAgentUsername">The display name of the assigned support agent, or null if the <see cref="Ticket"/> is unassigned.</param>
    public record TicketSummaryDto(
        Guid Id, 
        string Title, 
        TicketStatus Status, 
        TicketPriority Priority, 
        DateTime CreatedAt,
        DateTime UpdatedAt, 
        string? AssignedAgentUsername
    );
}
