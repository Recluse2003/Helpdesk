using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.CreateTicket
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for creating a new support ticket. 
    /// </summary>
    public sealed class CreateTicketEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/tickets", Handle)
                .WithTags("Tickets")
                .WithName("CreateTicket")
                .Produces<CreateTicketResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireAuthorization(policy =>
                    policy.RequireRole(RoleNames.Customer));
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            [FromBody] CreateTicketRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var command = new CreateTicketCommand(
                userId,
                request.Title,
                request.Description,
                request.Priority);

            var result = await sender.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return result.ToHttpResult();

            return Results.Created($"/api/tickets/{result.Value}", new CreateTicketResponse(result.Value));
        }
    }

    public record CreateTicketRequest(string Title, string Description, TicketPriority Priority);

    public record CreateTicketResponse(Guid TicketId);
}
