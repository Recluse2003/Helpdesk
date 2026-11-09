using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.ChangeTicketPriority
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for changing the priority of a support ticket. 
    /// </summary>
    public sealed class ChangeTicketPriorityEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/tickets/{ticketId:guid}/priority", Handle)
                .WithTags("Tickets")
                .WithName("ChangeTicketPriority")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .RequireAuthorization(policy =>
                    policy.RequireRole(RoleNames.SupportAgent, RoleNames.Administrator));
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            Guid ticketId,
            [FromBody] ChangeTicketPriorityRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var command = new ChangeTicketPriorityCommand(userId, ticketId, request.Priority);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }

    public record ChangeTicketPriorityRequest(TicketPriority Priority);
}
