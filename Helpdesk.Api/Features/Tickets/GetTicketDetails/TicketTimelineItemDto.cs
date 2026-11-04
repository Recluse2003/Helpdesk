using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Tickets.GetTicketDetails
{
    /// <summary>
    /// Represents an individual <see cref="TicketMessage"/> or <see cref="TicketActivity"/> displayed in a 
    /// <see cref="Ticket"/>'s timeline.
    /// </summary>
    /// <param name="Id">The unique identifier of the <see cref="TicketMessage"/> or <see cref="TicketActivity"/>.</param>
    /// <param name="Type">Indicates whether the timeline item is a <see cref="TicketMessage"/> or an <see cref="TicketActivity"/>.</param>
    /// <param name="ActorName">The display name of the user who performed the action or authored the <see cref="TicketMessage"/>,
    /// or System for system-generated activities.</param>
    /// <param name="CreatedAt">The date and time the timeline item was created.</param>
    /// <param name="Content">The <see cref="TicketMessage"/> content, or null when the item represents an 
    /// <see cref="TicketActivity"/>.</param>
    /// <param name="Details">A description of the <see cref="TicketActivity"/>, or null when the item represents 
    /// a <see cref="TicketMessage"/>.</param>
    /// <param name="ActivityType">The specific <see cref="TicketActivity"/> type, or null when the item represents 
    /// a <see cref="TicketMessage"/>.</param>
    public record TicketTimelineItemDto(
        Guid Id,
        TicketTimelineItemType Type,
        string ActorName,
        DateTime CreatedAt,
        string? Content,
        string? Details,
        TicketActivityType? ActivityType
    );
}