using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Tickets.GetTicketDetails
{
    public record TicketTimelineItemDto(
        Guid Id,
        TicketTimelineItemType Type,
        string ActorName,
        DateTime CreatedAt,
        string? Content,
        string? Details,
        TicketActivityType? ActivityType);
}