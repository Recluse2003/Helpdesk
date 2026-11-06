using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.AddTicketMessage
{
    public class AddTicketMessageEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/tickets/{ticketId:guid}/message", Handle)
                .WithTags("Tickets")
                .WithName("AddMessage")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .RequireAuthorization();
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            Guid ticketId,
            AddTicketMessageRequest request,
            TicketPriority newPriority,
            ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var command = new AddTicketMessageCommand(userId, ticketId, request.Content, request.IsPublic);

            var result = await sender.Send(command, cancellationToken);

            return result.ToNoContentResult();
        }
    }

    public record AddTicketMessageRequest(string Content, bool IsPublic);
}
