using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Escalations.GetEscalationDetails
{
    /// <summary>
    /// Represents the detailed information returned for a <see cref="Ticket"/>'s <see cref="EscalationRequest"/>. 
    /// </summary>
    /// <param name="Id">The unique identifier of the <see cref="EscalationRequest"/>.</param>
    /// <param name="TicketId">The unique identifier of the <see cref="Ticket"/> associated with the <see cref="EscalationRequest"/>.</param>
    /// <param name="TicketTitle">The title of the <see cref="Ticket"/> associated with the <see cref="EscalationRequest"/>.</param>
    /// <param name="RequestedByUserId">The unique identifier of the user who submitted the <see cref="EscalationRequest"/>.</param>
    /// <param name="RequestedByDisplayName">The display name of the user who submitted the <see cref="EscalationRequest"/>.</param>
    /// <param name="ProposedRecipientUserId">The unique identifier of the proposed recipient, if one was specified.</param>
    /// <param name="ProposedRecipientDisplayName">The display name of the proposed recipient, if available.</param>
    /// <param name="Reason">The reason provided for why the escalation was requested.</param>
    /// <param name="Status">The current status of the <see cref="EscalationRequest"/>.</param>
    /// <param name="RespondedByUserId">The unique idenifier of the admin who responded to the request, if applicable.</param>
    /// <param name="RespondedByDisplayName">The display name of the administrator who responded, if available.</param>
    /// <param name="NewSupportAgentId">The unique identifier of the support agent assigned as a result of the escalation, if accepted.</param>
    /// <param name="NewSupportAgentDisplayName">The display name of the newly assigned support agent, if available.</param>
    /// <param name="ResponseReason">The reason provided by the administrator when responding to the request, if applicable.</param>
    /// <param name="CreatedAt">The date and time the escalation request was created.</param>
    /// <param name="RespondedAt">The date and time the escalation request was responded to, if applicable.</param>
    public record EscalationRequestDetailsDto(
        Guid Id,
        Guid TicketId,
        string TicketTitle,

        Guid RequestedByUserId,
        string RequestedByDisplayName,

        Guid? ProposedRecipientUserId,
        string? ProposedRecipientDisplayName,

        string Reason,

        EscalationRequestStatus Status,

        Guid? RespondedByUserId,
        string? RespondedByDisplayName,

        Guid? NewSupportAgentId,
        string? NewSupportAgentDisplayName,

        string? ResponseReason,

        DateTime CreatedAt,
        DateTime? RespondedAt
    );
}
