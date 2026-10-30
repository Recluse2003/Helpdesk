using Helpdesk.Api.Shared.Interfaces;
using MediatR;

namespace Helpdesk.Api.Features.Tickets.GetTicketsByUserId
{
    public class GetTicketsByUserIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/{userId:guid}/tickets", async (
                Guid userId,
                int pageNumber,
                int pageSize,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new GetTicketsByUserIdQuery(userId, pageNumber, pageSize);
                var result = await mediator.Send(query, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.BadRequest(result.Error);
            });
        }
    }
}
