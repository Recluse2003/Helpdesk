using Helpdesk.Api.Domain.Enums;

namespace Helpdesk.Api.Features.Escalations.GetEscalationDetails
{
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
