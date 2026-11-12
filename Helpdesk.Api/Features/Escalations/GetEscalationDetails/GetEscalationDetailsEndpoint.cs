using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Escalations.GetEscalationDetails
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for retrieving the details of an existing escalation request. 
    /// </summary>
    public class GetEscalationDetailsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/escalations/{id:guid}", Handle)
                .WithTags("Escalations")
                .WithName("GetEscalationDetails")
                .Produces<EscalationRequestDetailsDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .RequireAuthorization(policy =>
                    policy.RequireRole(RoleNames.Administrator, RoleNames.SupportAgent));
        }

        private static async Task<IResult> Handle(
            Guid id,
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var query = new GetEscalationDetailsQuery(userId, id);

            var result = await sender.Send(query, cancellationToken);

            return result.ToHttpResult();
        }
    }
}
