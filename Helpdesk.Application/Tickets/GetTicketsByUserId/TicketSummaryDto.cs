using Helpdesk.Domain.Entities;
using Helpdesk.Domain.Enums;

namespace Helpdesk.Application.Tickets.GetTicketsByUserId
{
    /// <summary>
    /// Contains a summary information about a <see cref="Ticket"/> for display in the user's personal tickets page.
    /// </summary>
    public record TicketSummaryDto(Guid Id, string Title, TicketStatus Status, TicketPriority Priority, DateTime CreatedAt, DateTime UpdatedAt);
}
