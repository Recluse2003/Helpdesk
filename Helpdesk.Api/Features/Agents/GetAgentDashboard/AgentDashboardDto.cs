using Helpdesk.Api.Domain.Entities;

namespace Helpdesk.Api.Features.Agents.GetAgentDashboard
{
    /// <summary>
    /// Represents the dashboard of a specified agent. 
    /// </summary>
    /// <param name="AssignedTicketCount">The number of <see cref="Ticket"/>s currently assigned to the specified agent that are 
    /// unresolved or not closed.</param>
    /// <param name="UnassignedTicketCount">The number of <see cref="Ticket"/>s currently unassigned.</param>
    /// <param name="AwaitingCustomerResponseCount">The number of <see cref="Ticket"/>s assigned to the specified agent, that they 
    /// are waiting for a customer response for.</param>
    /// <param name="TicketsNeedingAttention">A collection containing summaries of the <see cref="Ticket"/>s currently assigned
    /// to the specified agent, that need attention.</param>
    /// <param name="RecentActivities">A collection containing recent activies performed to the specified agent's assigned 
    /// <see cref="Ticket"/>s.</param>
    public record AgentDashboardDto(
        int AssignedTicketCount,
        int UnassignedTicketCount,
        int AwaitingCustomerResponseCount,
        IReadOnlyCollection<AgentDashboardTicketDto> TicketsNeedingAttention,
        IReadOnlyCollection<AgentRecentActivityDto> RecentActivities
    );
}
