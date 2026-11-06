using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Features.Tickets.CreateTicket;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Escalations.RequestEscalation
{
    public class RequestEscalationEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/escalations", Handle)
                .WithTags("Escalations")
                .WithName("CreateEscalation")
                .Produces<RequestEscalationResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .RequireAuthorization(policy =>
                    policy.RequireRole(RoleNames.SupportAgent));
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            RequestEscalationRequest request,
            ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var command = new RequestEscalationCommand(userId, request.Reason, request.TicketId, request.ProposedRecipientUserId);

            var result = await sender.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return result.ToHttpResult();

            return Results.Created($"/api/escalations/{result.Value}", new RequestEscalationResponse(result.Value));
        }
    }

    public record RequestEscalationRequest(string Reason, Guid TicketId, Guid? ProposedRecipientUserId);

    public record RequestEscalationResponse(Guid EscalationRequestId);
}
