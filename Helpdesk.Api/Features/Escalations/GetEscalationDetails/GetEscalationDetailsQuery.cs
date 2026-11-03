using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using MediatR;

namespace Helpdesk.Api.Features.Escalations.GetEscalationDetails
{
    /// <summary>
    /// Represents a request to retrieve the details of a specified <see cref="EscalationRequest"/>
    /// </summary>
    /// <param name="UserId">Unique identifier of the user who is attempting to retrieve the specified
    /// <see cref="EscalationRequest"/></param> details. Only the requester of the escalation, or an admin can view a request.
    /// <param name="EscalationRequestId">Unique identifier of the <see cref="EscalationRequest"/> being retrieved.</param>
    /// <returns>
    /// A <see cref="Result"/> on success containing a <see cref="EscalationRequestDetailsDto"/>, or a <see cref="Result"/> 
    /// containing an <see cref="Error"/> describing why the operation failed.
    /// </returns>
    public record GetEscalationDetailsQuery(Guid UserId, Guid EscalationRequestId) : IRequest<Result<EscalationRequestDetailsDto>>;
}
