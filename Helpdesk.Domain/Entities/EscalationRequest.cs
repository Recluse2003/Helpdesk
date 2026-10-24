using Helpdesk.Domain.Enums;

namespace Helpdesk.Domain.Entities
{
    public class EscalationRequest
    {
        public Guid Id { get; private set; }

        public string Reason { get; private set; } = string.Empty;

        public string? ResponseReason { get; private set; }

        public Guid TicketId { get; private set; }

        public Guid RequestedByUserId { get; private set; }

        public Guid ProposedRecipientUserId { get; private set; }

        public Guid? RespondedByUserId { get; private set; }

        public EscalationRequestStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? RespondedAt { get; private set; } 
        
        public EscalationRequest(Guid id, string reason, Guid ticketId, Guid requestedByUserId, Guid proposedRecipientUserId)
        {
            Id = id;
            Reason = reason;
            TicketId = ticketId;
            RequestedByUserId = requestedByUserId;
            ProposedRecipientUserId = proposedRecipientUserId;
            Status = EscalationRequestStatus.Waiting;
            CreatedAt = DateTime.UtcNow;
        }

    }
}
