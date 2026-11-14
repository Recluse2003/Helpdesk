using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Auth.GetCurrentUser
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for retrieving details of the current user. 
    /// </summary>
    public class GetCurrentUserEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/auth/me", Handle)
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

    /// <summary>
    /// Represents the response for a <see cref="GetCurrentUserQuery"/>.
    /// </summary>
    /// <param name="Id">Unique identifier of the current user.</param>
    /// <param name="Email">Email address of the current user.</param>
    /// <param name="DisplayName">Display name of the current user.</param>
    /// <param name="Roles">Roles of the current user.</param>
    public record GetCurrentUserResponse(
        Guid Id,
        string Email,
        string DisplayName,
        IReadOnlyList<string> Roles);
}
