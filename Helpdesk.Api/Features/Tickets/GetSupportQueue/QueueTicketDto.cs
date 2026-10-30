using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Tickets.GetSupportQueue
{
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
        string? AssigneeDisplayName);
}