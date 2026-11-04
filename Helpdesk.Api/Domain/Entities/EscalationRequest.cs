using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Api.Domain.Entities
{
    /// <summary>
    /// Represents a request to escalate a support ticket to another support agent.
    /// </summary>
    /// <remarks>
    /// Tracks the reason for escalation, the requesting user, any proposed recipient and the administrator's response. 
    /// An escalation request can be accepted or rejected only once.
    /// </remarks>
    public class EscalationRequest
    {
        /// <summary>
        /// Gets the unique identifier of the escalation request.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the reason provided for requesting the escalation.
        /// </summary>
        public string Reason { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the optional explanation provided when responding to the escalation request.
        /// </summary>
        public string? ResponseReason { get; private set; }

        /// <summary>
        /// Gets the unique identifier of the ticket associated with the escalation request.
        /// </summary>
        public Guid TicketId { get; private set; }

        /// <summary>
        /// Gets the ticket associated with the escalation request.
        /// </summary>
        public Ticket? Ticket { get; private set; }

        /// <summary>
        /// Gets the unique identifier of the user who requested the escalation.
        /// </summary>
        public Guid RequestedByUserId { get; private set; }

        /// <summary>
        /// Gets the unique identifier of the proposed recipient, if one was specified.
        /// </summary>
        public Guid? ProposedRecipientUserId { get; private set; }

        /// <summary>
        /// Gets the unique identifier of the user who responded to the escalation request,
        /// or null if it has not yet been answered.
        /// </summary>
        public Guid? RespondedByUserId { get; private set; }

        /// <summary>
        /// Gets the unique identifier of the support agent assigned to the ticket
        /// when the escalation was accepted, or null if it has not been accepted.
        /// </summary>
        public Guid? NewSupportAgentId { get; private set; }

        /// <summary>
        /// Gets the current status of the escalation request.
        /// </summary>
        public EscalationRequestStatus Status { get; private set; }

        /// <summary>
        /// Gets the date and time the escalation request was created.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets the date and time the escalation request was answered,
        /// or null if it is still awaiting a response.
        /// </summary>
        public DateTime? RespondedAt { get; private set; }

        /// <summary>
        /// Creates a new escalation request.
        /// </summary>
        /// <param name="reason">The reason for requesting the escalation.</param>
        /// <param name="ticketId">The unique identifier of the ticket being escalated.</param>
        /// <param name="requestedByUserId">The unique identifier of the user requesting the escalation.</param>
        /// <param name="proposedRecipientUserId">The optional unique identifier of the proposed recipient.</param>
        /// <exception cref="DomainException">
        /// Thrown when the reason is missing or exceeds 5000 characters, or when the ticket or requesting user identifier 
        /// is empty.
        /// </exception>
        public EscalationRequest(string reason, Guid ticketId, Guid requestedByUserId, Guid? proposedRecipientUserId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Escalation reason is required.");

            if (reason.Length > 5000)
                throw new DomainException("Escalation reason cannot exceed 5000 characters.");

            if (ticketId == Guid.Empty)
                throw new DomainException("A ticket must be specified.");

            if (requestedByUserId == Guid.Empty)
                throw new DomainException("A requesting user must be specified.");

            if (requestedByUserId == proposedRecipientUserId)
                throw new DomainException("A requesting user can not propose to escalate a ticket to themself.");

            Id = Guid.NewGuid();
            Reason = reason;
            TicketId = ticketId;
            RequestedByUserId = requestedByUserId;
            ProposedRecipientUserId = proposedRecipientUserId;
            Status = EscalationRequestStatus.Waiting;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Accepts the escalation request and records the administrator's response.
        /// </summary>
        /// <param name="respondedByUserId"></param>
        /// <param name="newSupportAgentId"></param>
        /// <param name="responseReason"></param>
        /// <exception cref="DomainException">
        /// Thrown when the escalation request has already been answered, or when responding user or new support agent ]
        /// identifier is empty, or when the response reason exceeds 5000 characters.
        /// </exception>
        public void Accept(Guid respondedByUserId, Guid newSupportAgentId, string? responseReason)
        {
            if (Status != EscalationRequestStatus.Waiting)
                throw new DomainException("This escalation request has already been responded to.");

            if (respondedByUserId == Guid.Empty)
                throw new DomainException("The responding user must be specified.");

            if (newSupportAgentId == Guid.Empty)
                throw new DomainException("The new support agent must be specified.");

            if (responseReason != null && responseReason.Length > 5000)
                throw new DomainException("Escalation response reason cannot exceed 5000 characters.");

            RespondedByUserId = respondedByUserId;
            NewSupportAgentId = newSupportAgentId;
            ResponseReason = responseReason;
            Status = EscalationRequestStatus.Accepted;
            RespondedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Rejects the escalation request and records the administrator's response.
        /// </summary>
        /// <param name="respondedByUserId">The unique identifier of the user rejecting the request.</param>
        /// <param name="responseReason">An optional explanation accompanying the rejection.</param>
        /// <exception cref="DomainException">
        /// Thrown when the escalation request has already been answered, or when the responding user identifier is empty
        /// or when the response reason exceeds 5000 characters.
        /// </exception>
        public void Reject(Guid respondedByUserId, string? responseReason)
        {
            if (Status != EscalationRequestStatus.Waiting)
                throw new DomainException("This escalation request has already been responded to.");

            if (respondedByUserId == Guid.Empty)
                throw new DomainException("The responding user must be specified.");

            if (responseReason != null && responseReason.Length > 5000)
                throw new DomainException("Escalation response reason cannot exceed 5000 characters.");

            RespondedByUserId = respondedByUserId;
            ResponseReason = responseReason;
            Status = EscalationRequestStatus.Rejected;
            RespondedAt = DateTime.UtcNow;
        }
    }
}
