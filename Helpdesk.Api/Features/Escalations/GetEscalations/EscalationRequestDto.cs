using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Escalations.GetEscalations
{
    /// <summary>
    /// Represents a summary of a <see cref="Ticket"/>'s <see cref="EscalationRequest"/>.
    /// </summary>
    /// <param name="Id">The unique identifier of the <see cref="EscalationRequest"/>.</param>
    /// <param name="TicketId">The unique identifier of the <see cref="Ticket"/> associated with the <see cref="EscalationRequest"/>.</param>
    /// <param name="TicketTitle">The title of the associated <see cref="Ticket"/>.</param>
    /// <param name="RequestedByUserId">The unique identifier of the support agent who submitted the request.</param>
    /// <param name="RequestedByDisplayName">The display name of the user who submitted the request.</param>
    /// <param name="ProposedRecipientUserId">The unique identifier of the proposed recipient, if specified.</param>
    /// <param name="ProposedRecipientDisplayName">The display name of the proposed recipient, if available.</param>
    /// <param name="Status">Current status of the <see cref="EscalationRequest"/>.</param>
    /// <param name="CreatedAt">The date and time the request was created.</param>
    /// <param name="RespondedAt">The date and time the request was responded to, if applicable.</param>
    public record EscalationRequestDto(
        Guid Id,
        Guid TicketId,
        string TicketTitle,
        Guid RequestedByUserId,
        string RequestedByDisplayName,
        Guid? ProposedRecipientUserId,
        string? ProposedRecipientDisplayName,
        EscalationRequestStatus Status,
        DateTime CreatedAt,
        DateTime? RespondedAt
    );
}