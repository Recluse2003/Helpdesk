using Helpdesk.Domain.Enums;

namespace Helpdesk.Application.Tickets.GetSupportQueue
{
    public record QueueTicketDto(
        Guid Id,
        string Title,
        TicketStatus Status,
        TicketPriority Priority,
        DateTime CreatedAt,
        Guid? AssignedToId,
        string? AssigneeName);
}