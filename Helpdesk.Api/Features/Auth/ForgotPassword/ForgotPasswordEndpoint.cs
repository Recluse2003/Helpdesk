using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Features.Auth.ForgotPassword
{
    public class ForgotPasswordEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/forgot-password", Handle)
                .WithTags("Authentication")
                .WithName("ForgotPassword")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest);
        }

        private static async Task<IResult> Handle(
            [FromBody] ForgotPasswordRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var command = new ForgotPasswordCommand(request.Email);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }

    public record ForgotPasswordRequest(string Email);
}
