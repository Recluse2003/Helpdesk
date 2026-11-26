using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Agents.GetAgentDashboard
{
    public class GetAgentDashboardEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/agents/dashboard", Handle)
                .WithTags("Agents")
                .WithName("GetDashboard")
                .Produces<AgentDashboardDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .RequireAuthorization(policy => policy.RequireRole(RoleNames.SupportAgent));
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var query = new GetAgentDashboardQuery(userId);

            var result = await sender.Send(query, cancellationToken);

            return result.ToHttpResult();
        }
    }
}
