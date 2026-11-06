using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.GetSupportQueue
{
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
            GetSupportQueueRequest request,
            ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetSupportQueueQuery(
                request.PageNumber,
                request.PageSize,
                request.Status,
                request.Priority,
                request.CustomerId,
                request.AssigneeId
            );

            var result = await mediator.Send(query, cancellationToken);

            return result.ToHttpResult();
        }
    }

    public record GetSupportQueueRequest(
        int PageNumber,
        int PageSize,
        TicketStatus? Status,
        TicketPriority? Priority,
        Guid? CustomerId,
        Guid? AssigneeId
    );
}
