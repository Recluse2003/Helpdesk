using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Contains a summary information about a <see cref="Ticket"/> for display in the user's personal tickets page.
    /// </summary>
    public record TicketSummaryDto(Guid Id, string Title, TicketStatus Status, TicketPriority Priority, DateTime CreatedAt, DateTime UpdatedAt, string? AssignedAgentUsername);
}
