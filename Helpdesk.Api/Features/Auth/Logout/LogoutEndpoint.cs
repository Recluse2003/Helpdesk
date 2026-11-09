using Helpdesk.Api.Features.Auth.Login;
using Helpdesk.Api.Shared.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Features.Auth.Logout
{
    public class LogoutEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/logout", Handle)
                .WithTags("Authentication")
                .WithName("Logout")
                .AllowAnonymous()
                .Produces<LoginResponseDto>(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest);
        }

        private static async Task<IResult> Handle(
            [FromServices] ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            httpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken);

            var result = await sender.Send(new LogoutCommand(refreshToken ?? string.Empty), cancellationToken);

            if (!result.IsSuccess)
                return result.ToHttpResult();

            httpContext.Response.Cookies.Delete(
                "refreshToken",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api/auth"
                });

            return Results.NoContent();
        }
    }
}
