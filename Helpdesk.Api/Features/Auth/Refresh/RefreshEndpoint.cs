using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Features.Auth.Refresh
{
    public class RefreshEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/refresh", Handle)
                .WithTags("Authentication")
                .WithName("Refresh")
                .AllowAnonymous()
                .Produces<RefreshResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized);
        }

        private static async Task<IResult> Handle(
            [FromServices] ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            if (!httpContext.Request.Cookies.TryGetValue(
                    "refreshToken", out var refreshToken) ||
                string.IsNullOrWhiteSpace(refreshToken))
            {
                return Results.Unauthorized();
            }

            var command = new RefreshCommand(refreshToken);

            var result = await sender.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return result.ToHttpResult();

            var response = result.Value;

            httpContext.Response.Cookies.Append(
                "refreshToken",
                response.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    Path = "/api/auth"
                });

            return Results.Ok(new RefreshResponse(response.AccessToken));
        }
    }

    public record RefreshResponse(string AccessToken);
}
