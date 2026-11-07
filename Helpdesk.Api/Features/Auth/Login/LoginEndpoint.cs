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
                .Produces<LoginResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized);
        }

        private static async Task<IResult> Handle(
            [FromBody] LoginRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var command = new LoginCommand(request.Email, request.Password);

            var result = await sender.Send(command, cancellationToken);

            return result.ToHttpResult();
        }

        public record LoginRequest(string Email, string Password);
    }
}
