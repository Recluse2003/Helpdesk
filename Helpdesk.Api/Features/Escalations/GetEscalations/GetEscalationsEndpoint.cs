using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Features.Escalations.GetEscalations
{
    /// <summary>
    /// Defines the HTTP endpoint responsible for retrieving a personalised paged list of escalation requests, based on the 
    /// filters provided. 
    /// </summary>
    public class GetEscalationsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/escalations", Handle)
               .WithTags("Escalations")
               .WithName("GetEscalations")
               .Produces<PagedList<EscalationRequestDto>>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status400BadRequest)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .RequireAuthorization(policy => policy.RequireRole(RoleNames.Administrator));
        }

        private static async Task<IResult> Handle(
            [AsParameters] GetEscalationsRequest request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetEscalationsQuery(
                request.PageNumber,
                request.PageSize,
                request.Status,
                request.TicketId,
                request.RequestedByUserId
            );

            var result = await mediator.Send(query, cancellationToken);

            return result.ToHttpResult();
        }
    }

    public record GetEscalationsRequest(
        int PageNumber = 1,
        int PageSize = 10,
        EscalationRequestStatus? Status = null,
        Guid? TicketId = null,
        Guid? RequestedByUserId = null
    );
}
