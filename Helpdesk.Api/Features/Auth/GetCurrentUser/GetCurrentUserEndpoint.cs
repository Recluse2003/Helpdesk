using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Features.Tickets.GetSupportQueue;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Auth.GetCurrentUser
{
    public class GetCurrentUserEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/users/me", Handle)
                .WithTags("Authentication")
                .WithName("CurrentUser")
                .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .RequireAuthorization();
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var query = new GetCurrentUserQuery(userId);

            var result = await sender.Send(query, cancellationToken);

            return result.ToHttpResult();
        }
    }
}
