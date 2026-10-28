using Helpdesk.Domain.Enums;

namespace Helpdesk.Application.Features.Tickets.GetSupportQueue
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