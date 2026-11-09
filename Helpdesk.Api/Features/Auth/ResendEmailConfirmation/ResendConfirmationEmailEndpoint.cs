using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Features.Auth.ResendEmailConfirmation
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for resending out account email confirmation. 
    /// </summary>
    public class ResendConfirmationEmailEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/resend-confirmation-email", Handle)
                .WithTags("Authentication")
                .WithName("ResendConfirmationEmail")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }

        private static async Task<IResult> Handle(
            [FromBody] ResendEmailConfirmationRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var command = new ResendConfirmationEmailCommand(request.Email);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }

    public record ResendEmailConfirmationRequest(string Email);
}
