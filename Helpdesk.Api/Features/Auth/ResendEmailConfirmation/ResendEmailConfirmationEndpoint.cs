using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Features.Auth.ResendEmailConfirmation
{
    public class ResendEmailConfirmationEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/resend-email-confirmation", Handle)
                .WithTags("Authentication")
                .WithName("ResendEmailConfirmation")
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
            var command = new ResendEmailConfirmationCommand(request.Email);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }

    public record ResendEmailConfirmationRequest(string Email);
}
