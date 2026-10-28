namespace Helpdesk.Application.Features.Tickets.GetTicketDetails
{
    public record TicketTimelineItemDto(
        Guid Id,
        TicketTimelineItemType Type,
        string ActorName,
        DateTime CreatedAt,
        string? Content,
        string? Details);
}