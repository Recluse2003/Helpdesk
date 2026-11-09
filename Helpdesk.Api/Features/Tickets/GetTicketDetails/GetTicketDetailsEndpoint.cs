using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.GetTicketDetails
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for retrieving the details of an existing support ticket. 
    /// </summary>
    public sealed class GetTicketDetailsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/tickets/{ticketId:guid}", Handle) 
                .WithTags("Tickets")
                .WithName("GetTicketDetails")
                .Produces<TicketDetailsDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .RequireAuthorization();
        }

        private static async Task<IResult> Handle(
            Guid ticketId,
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var query = new GetTicketDetailsQuery(userId, ticketId);

            var result = await sender.Send(query, cancellationToken);

            return result.ToHttpResult();
        }
    }
}
