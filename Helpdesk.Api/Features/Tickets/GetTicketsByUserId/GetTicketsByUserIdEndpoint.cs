using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    public class GetTicketsByUserIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/me/tickets", async (
                ClaimsPrincipal user,
                int pageNumber,
                int pageSize,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var query = new GetTicketsByUserIdQuery(userId, pageNumber, pageSize);
git 
                var result = await mediator.Send(query, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.BadRequest(result.Error);
            })
            .RequireAuthorization();
        }
    }
}
