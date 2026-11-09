using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Features.Auth.ResetPassword
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for resetting an account's password. 
    /// </summary>
    public class ResetPasswordEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/reset-password", Handle)
                .WithTags("Authentication")
                .WithName("ResetPassword")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }

        private static async Task<IResult> Handle(
            [AsParameters] ResetPasswordRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var command = new ResetPasswordCommand(request.UserId, request.Code, request.NewPassword);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }

    public record ResetPasswordRequest(Guid UserId, string Code, string NewPassword);
}
