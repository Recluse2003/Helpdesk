using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Tickets.GetTicketDetails
{
    /// <summary>
    /// Represents the details of a <see cref="Ticket"/>, including its current state and timeline.
    /// </summary>
    /// <param name="Id">The unique identifier of the <see cref="Ticket"/>.</param>
    /// <param name="Title">The title of the <see cref="Ticket"/>.</param>
    /// <param name="Description">The description of the issue reported.</param>
    /// <param name="Status">The current status of the <see cref="Ticket"/>.</param>
    /// <param name="Priority">The priority  of the <see cref="Ticket"/>.</param>
    /// <param name="CreatedAt">The date and time the <see cref="Ticket"/> was created.</param>
    /// <param name="UpdatedAt">The date and time the <see cref="Ticket"/> was last updated.</param>
    /// <param name="Timeline">The chronological collection of messages and activities associated with the <see cref="Ticket"/>.</param>
    public record TicketDetailsDto(
        Guid Id,
        string Title,
        string Description,
        TicketStatus Status,
        TicketPriority Priority,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<TicketTimelineItemDto> Timeline
    );
}