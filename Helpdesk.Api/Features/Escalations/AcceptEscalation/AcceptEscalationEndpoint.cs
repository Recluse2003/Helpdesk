using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Escalations.AcceptEscalation
{
    public class AcceptEscalationEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/escalations/{escalationRequestId:guid}/accept", Handle)
               .WithTags("Escalations")
               .WithName("AcceptEscalation")
               .Produces(StatusCodes.Status204NoContent)
               .ProducesProblem(StatusCodes.Status400BadRequest)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .ProducesProblem(StatusCodes.Status409Conflict)
               .RequireAuthorization(policy =>
                   policy.RequireRole(RoleNames.Administrator));
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            Guid escalationRequestId,
            [FromBody] AcceptEscalationRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var command = new AcceptEscalationCommand(userId, escalationRequestId, request.NewSupportAgentId, request.ResponseReason);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }

    }

    public record AcceptEscalationRequest(Guid NewSupportAgentId, string? ResponseReason);
}