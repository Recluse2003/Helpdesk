using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Api.Domain.Entities
{
    public class EscalationRequest
    {
        public Guid Id { get; private set; }

        public string Reason { get; private set; } = string.Empty;

        public string? ResponseReason { get; private set; }

        public Guid TicketId { get; private set; }

        public Ticket? Ticket { get; private set; }

        public Guid RequestedByUserId { get; private set; }

        public Guid? ProposedRecipientUserId { get; private set; }

        public Guid? RespondedByUserId { get; private set; }

        public Guid? NewSupportAgentId { get; private set; }

        public EscalationRequestStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? RespondedAt { get; private set; } 
        
        public EscalationRequest(string reason, Guid ticketId, Guid requestedByUserId, Guid? proposedRecipientUserId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Escalation reason is required.", nameof(reason));

            if (reason.Length > 5000)
                throw new ArgumentException("Escalation reason cannot exceed 5000 characters.", nameof(reason));

            if (ticketId == Guid.Empty)
                throw new ArgumentException("A ticket must be specified.", nameof(ticketId));

            Id = Guid.NewGuid();
            Reason = reason;
            TicketId = ticketId;
            RequestedByUserId = requestedByUserId;
            ProposedRecipientUserId = proposedRecipientUserId;
            Status = EscalationRequestStatus.Waiting;
            CreatedAt = DateTime.UtcNow;
        }

        public void Accept(Guid respondedByUserId, Guid newSupportAgentId, string? responseReason)
        {
            if (Status != EscalationRequestStatus.Waiting)
                throw new DomainException("This escalation request has already been responded to.");

            if (respondedByUserId == Guid.Empty)
                throw new ArgumentException("The responding user must be specified.", nameof(respondedByUserId));

            if (newSupportAgentId == Guid.Empty)
                throw new ArgumentException("The new support agent must be specified.", nameof(newSupportAgentId));

            if (responseReason != null && responseReason.Length > 5000)
                throw new ArgumentException("Escalation response reason cannot exceed 5000 characters.", nameof(responseReason));

            RespondedByUserId = respondedByUserId;
            NewSupportAgentId = newSupportAgentId;
            ResponseReason = responseReason;
            Status = EscalationRequestStatus.Accepted;
            RespondedAt = DateTime.UtcNow;
        }

        public void Reject(Guid respondedByUserId, string? responseReason)
        {
            if (Status != EscalationRequestStatus.Waiting)
                throw new DomainException("This escalation request has already been responded to.");

            if (respondedByUserId == Guid.Empty)
                throw new ArgumentException("The responding user must be specified.", nameof(respondedByUserId));

            if (responseReason != null && responseReason.Length > 5000)
                throw new ArgumentException("Escalation response reason cannot exceed 5000 characters.", nameof(responseReason));

            RespondedByUserId = respondedByUserId;
            ResponseReason = responseReason;
            Status = EscalationRequestStatus.Rejected;
            RespondedAt = DateTime.UtcNow;
        }
    }
}
