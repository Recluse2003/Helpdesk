using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Helpdesk.Api.Features.Tickets.GetSupportQueue
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for retrieving a personalised paged list of tickets, based on the 
    /// filters provided. 
    /// </summary>
    public sealed class GetSupportQueueEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/tickets/queue", Handle)
                .WithTags("Tickets")
                .WithName("GetSupportQueue")
                .Produces<PagedList<QueueTicketDto>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .RequireAuthorization(policy => policy.RequireRole(RoleNames.SupportAgent, RoleNames.Administrator));
        }

        private static async Task<IResult> Handle(
            ClaimsPrincipal user,
            [AsParameters] GetSupportQueueRequest request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var query = new GetSupportQueueQuery(
                PageNumber: request.PageNumber,
                PageSize: request.PageSize,
                Status: request.Status,
                Priority: request.Priority,
                SearchTerm: request.SearchTerm,
                AssigneeId: request.AssignedToMe ? userId : request.AssigneeId,
                UnassignedOnly: request.UnassignedOnly,
                SortBy: request.SortBy,
                SortDescending: request.SortDescending
            );

            var result = await mediator.Send(query, cancellationToken);

            return result.ToHttpResult();
        }
    }

    public record GetSupportQueueRequest(
        int PageNumber = 1,
        int PageSize = 10,
        TicketStatus? Status = null,
        TicketPriority? Priority = null,
        string? SearchTerm = null,
        Guid? AssigneeId = null,
        bool AssignedToMe = false,
        bool UnassignedOnly = false,
        TicketSortBy SortBy = TicketSortBy.UpdatedAt,
        bool SortDescending = true
    );
}
