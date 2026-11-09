using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.AssignTicket
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for allowing administrators to assign support tickets to support agents. 
    /// </summary>
    public class AssignTicketEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/tickets/{ticketId:guid}/assign", Handle)
                .WithTags("Tickets")
                .WithName("AssignTicket")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .RequireAuthorization(policy => policy.RequireRole(RoleNames.Administrator));
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            Guid ticketId,
            [FromBody] AssignTicketRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var command = new AssignTicketCommand(userId, ticketId, request.AssigneeId);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }

    public record AssignTicketRequest(Guid AssigneeId);
}
