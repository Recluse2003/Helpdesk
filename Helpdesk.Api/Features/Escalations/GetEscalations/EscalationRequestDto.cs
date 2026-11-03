using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Escalations.GetEscalations
{
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