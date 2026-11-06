using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Escalations.RejectEscalation
{
    public class RejectEscalationEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/escalations/{escalationRequestId:guid}/reject", Handle)
                .WithTags("Escalations")
                .WithName("RejectEscalation")
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
            RejectEscalationRequest request,
            ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var command = new RejectEscalationCommand(userId, escalationRequestId, request.ResponseReason);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }

    public record RejectEscalationRequest(string? ResponseReason);
}
