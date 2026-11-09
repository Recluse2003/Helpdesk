using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Features.Auth.ConfirmEmail
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for account email confirmation. 
    /// </summary>
    public class ConfirmEmailEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/confirm-email", Handle)
                .WithTags("Authentication")
                .WithName("ConfirmEmail")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }

        private static async Task<IResult> Handle(
            [FromQuery] Guid userId,
            [FromQuery] string code, 
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var command = new ConfirmEmailCommand(userId, code);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }
}
