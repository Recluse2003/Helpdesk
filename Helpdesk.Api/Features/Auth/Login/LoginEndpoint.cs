using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Features.Auth.Login
{
    public class LoginEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/login", Handle)
                .WithTags("Authentication")
                .WithName("Login")
                .AllowAnonymous()
                .Produces<LoginResponseDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }

        private static async Task<IResult> Handle(
            [FromBody] LoginRequest request,
            [FromServices] ISender sender,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            var command = new LoginCommand(request.Email, request.Password);

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

            return Results.Ok(new LoginResponse(response.AccessToken));
        }

        public record LoginRequest(string Email, string Password);
        public record LoginResponse(string AccessToken);
    }
}
