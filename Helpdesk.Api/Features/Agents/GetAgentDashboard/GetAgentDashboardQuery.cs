using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Agents.GetAgentDashboard
{
    /// <summary>
    /// Represents a request to retrieve the dashboard of the current user. 
    /// </summary>
    /// <param name="AgentId">The unique identifier of the support agent.</param>
    public record GetAgentDashboardQuery(Guid AgentId) : IRequest<Result<AgentDashboardDto>>;
}
