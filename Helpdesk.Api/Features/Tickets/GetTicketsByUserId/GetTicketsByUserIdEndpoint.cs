using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    public sealed class GetTicketsByUserIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/me/tickets", Handle)
                .WithTags("Tickets")
                .WithName("GetMyTickets")
                .Produces<PagedList<TicketSummaryDto>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .RequireAuthorization();
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            [AsParameters] GetTicketsByUserIdRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var query = new GetTicketsByUserIdQuery(userId, request.PageNumber, request.PageSize);

            var result = await sender.Send(query, cancellationToken);

            return result.ToHttpResult();
        }
    }

    public record GetTicketsByUserIdRequest(int PageNumber, int PageSize);
}
