using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Features.Tickets.GetTicketsByUserId;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.GetTicketDetails
{
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
            ISender sender,
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
