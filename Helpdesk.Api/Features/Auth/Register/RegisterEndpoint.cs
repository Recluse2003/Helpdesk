using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity.Data;
using System.Reflection.Metadata;

namespace Helpdesk.Api.Features.Auth.Register
{
    public class RegisterEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/register", Handle)
            .WithTags("Authentication")
            .WithName("Register")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        }

        private static async Task<IResult> Handle(
            RegisterRequest request,
            ISender sender,
            CancellationToken cancellationToken)
        {
            var command = new RegisterCommand(
                request.DisplayName,
                request.Email,
                request.Password);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }

    public record RegisterRequest(string DisplayName, string Email, string Password);
}
