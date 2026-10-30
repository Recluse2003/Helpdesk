using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Tickets.GetTicketDetails
{
    public record TicketDetailsDto(
        Guid Id,
        string Title,
        string Description,
        TicketStatus Status,
        TicketPriority Priority,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<TicketTimelineItemDto> Timeline);
}