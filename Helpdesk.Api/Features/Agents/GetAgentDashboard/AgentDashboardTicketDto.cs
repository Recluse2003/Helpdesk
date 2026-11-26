using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Agents.GetAgentDashboard
{
    /// <summary>
    /// Represents the summary of a ticket assigned to a specified agent. 
    /// </summary>
    /// <param name="Id">The unique identifier of the <see cref="Ticket"/>.</param>
    /// <param name="Title">The title of the <see cref="Ticket"/>.</param>
    /// <param name="Priority">The priority of the <see cref="Ticket"/>.</param>
    /// <param name="Status">The current status of the <see cref="Ticket"/>.</param>
    /// <param name="CreatedAt">The date and time the ticket was created at. </param>
    /// <param name="UpdatedAt">The date and time the ticket was last updated. </param>
    /// <param name="CustomerId">The unique identifier of the user who submitted the ticket.</param>
    /// <param name="CustomerDisplayName">The display name of the user who submitted the ticket.</param>
    /// <param name="CustomerImageUrl">The url to the profile image of the user who submitted the ticket.</param>
    public record AgentDashboardTicketDto(
        Guid Id,
        string Title,
        TicketPriority Priority,
        TicketStatus Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        Guid CustomerId,
        string CustomerDisplayName,
        string? CustomerImageUrl
    );
}
