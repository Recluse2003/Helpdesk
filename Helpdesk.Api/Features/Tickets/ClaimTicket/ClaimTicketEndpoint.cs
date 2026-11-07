using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.ClaimTicket
{
    public sealed class ClaimTicketEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/tickets/{ticketId:guid}/claim", Handle)
                .WithTags("Tickets")
                .WithName("ClaimTicket")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .RequireAuthorization(policy =>
                    policy.RequireRole(RoleNames.SupportAgent));
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            Guid ticketId,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var command = new ClaimTicketCommand(userId, ticketId);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }
}
