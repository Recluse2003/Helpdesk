using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Api.Domain.Entities
{
    /// <summary>
    /// Represents a support ticket. This class managaes its lifecycle, assignment messages, activities and escalation requests.
    /// </summary>
    public class Ticket
    {
        /// <summary>
        /// Gets the unique identifier of the ticket.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the title of the ticket.
        /// </summary>
        public string Title { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the description of the issue reported in the ticket.
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the current status of the ticket.
        /// </summary>
        public TicketStatus Status { get; private set; }

        /// <summary>
        /// Gets the priority level of the ticket.
        /// </summary>
        public TicketPriority Priority { get; private set; }

        /// <summary>
        /// Gets the unique identifier of the user who created the ticket.
        /// </summary>
        public Guid CreatedById { get; private set; }

        /// <summary>
        /// Gets the unique identifier of the support agent assigned to the ticket,
        /// or null if the ticket is unassigned.
        /// </summary>
        public Guid? AssignedToId { get; private set; }

        /// <summary>
        /// Gets the date and time the ticket was created.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets the date and time the ticket was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; private set; }

        private readonly List<TicketMessage> _messages = [];
        private readonly List<TicketActivity> _activities = [];
        private readonly List<EscalationRequest> _escalationRequests = [];

        /// <summary>
        /// Gets the messages associated with the ticket.
        /// </summary>
        public IReadOnlyCollection<TicketMessage> Messages => _messages.AsReadOnly();

        /// <summary>
        /// Gets the activities recorded against the ticket.
        /// </summary>
        public IReadOnlyCollection<TicketActivity> Activities => _activities.AsReadOnly();

        /// <summary>
        /// Gets the escalation requests associated with the ticket.
        /// </summary>
        public IReadOnlyCollection<EscalationRequest> EscalationRequests => _escalationRequests.AsReadOnly();

        /// <summary>
        /// Creates a new support ticket.
        /// </summary>
        /// <param name="title">The title of the ticket.</param>
        /// <param name="description">The description of the ticket.</param>
        /// <param name="createdById">The unique identifier of the user creating the ticket.</param>
        /// <param name="priority">The initial priority of the ticket, defined by the user. Defaults to Medium.</param>
        /// <exception cref="DomainException">
        /// Thrown when the title or description is invalid, the creator identifier is empty, or the supplied values exceed
        /// their permitted lengths. 
        /// </exception>
        public Ticket(string title, string description, Guid createdById, TicketPriority priority = TicketPriority.Medium)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Ticket title is required.");

            if (title.Length > 200)
                throw new DomainException("Ticket title cannot exceed 200 characters.");

            if (string.IsNullOrWhiteSpace(description))
                throw new DomainException("Ticket description is required.");

            if (description.Length > 5000)
                throw new DomainException("Ticket description cannot exceed 5000 characters.");

            if (createdById == Guid.Empty)
                throw new DomainException("A ticket must specify a creator.");

            Id = Guid.NewGuid();
            Title = title.Trim();
            Description = description.Trim();
            CreatedById = createdById;
            Priority = priority;
            Status = TicketStatus.Open;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;

            var activity = new TicketActivity(Id, TicketActivityType.Created, createdById);
            AddActivity(activity);
        }

        public void AddMessage(TicketMessage message)
        {
            _messages.Add(message);
        }

        public void AddActivity(TicketActivity activity)
        {
            _activities.Add(activity);
        }

        /// <summary>
        /// Claims the ticket for the specified support agent. 
        /// </summary>
        /// <param name="agentId">The unique identifier of the support agent claiming the ticket.</param>
        /// <param name="agentDisplayName">The display name of the support agent claiming the ticket.</param>
        /// <exception cref="DomainException">
        /// Thrown when the agent details are invalid or the ticket is already assigned.
        /// </exception>
        /// <remarks>
        /// Used by a support agent to assign this ticket to themself. 
        /// </remarks>
        public void Claim(Guid agentId, string agentDisplayName)
        {
            if (agentId == Guid.Empty)
                throw new DomainException("A valid user ID must be specified to assign the ticket.");

            if (string.IsNullOrWhiteSpace(agentDisplayName))
                throw new DomainException("A valid display name must be specified.");

            if (AssignedToId != null)
                throw new DomainException("This ticket has already been assigned.");

            AssignedToId = agentId;
            UpdatedAt = DateTime.UtcNow;

            AddActivity(new TicketActivity(
                Id,
                TicketActivityType.Assigned,
                agentId,
                $"Ticket claimed by {agentDisplayName}."));
        }

        /// <summary>
        /// Assigns the ticket to a support agent or reassigns it from its current agent. 
        /// </summary>
        /// <param name="assignerId">The unique identifier of the user performing the assignment.</param>
        /// <param name="assignerDisplayName">The display name of the user performing the assignment.</param>
        /// <param name="assigneeId">The unique identifier of the support agent being assigned the ticket.</param>
        /// <param name="assigneeDisplayName">The display name of the support agent being assigned the ticket.</param>
        /// <param name="oldAssigneeDisplayName">The display name of the previously assigned agent, if applicable.</param>
        /// <exception cref="DomainException">
        /// Thrown when the supplied user identifiers or display names are invalid.
        /// </exception>
        /// <remarks>
        /// Used by an administrator to assign the ticket to a support agent. 
        /// </remarks>
        public void AssignTo(
            Guid assignerId, 
            string assignerDisplayName,
            Guid assigneeId, 
            string assigneeDisplayName, 
            string? oldAssigneeDisplayName)
        {
            if (AssignedToId == assigneeId)
                return;

            if (assignerId == Guid.Empty)
                throw new DomainException("A valid user ID must be specified to assign the ticket.");

            if (string.IsNullOrWhiteSpace(assignerDisplayName))
                throw new DomainException("A valid assigner display name must be specified.");

            if (assigneeId == Guid.Empty)
                throw new DomainException("A valid user ID must be specified to assign the ticket to.");

            if (string.IsNullOrWhiteSpace(assigneeDisplayName))
                throw new DomainException("A valid assignee display name must be specified.");

            // Determines the details recorded as a ticket activity, based on if the ticket is being reassigned from a 
            // previous support agent. 
            string details = AssignedToId == null ?
                $"{assignerDisplayName} assigned the ticket to {assigneeDisplayName}." :
                $"{assignerDisplayName} reassigned the ticket from {oldAssigneeDisplayName} to {assigneeDisplayName}.";

            AssignedToId = assigneeId;
            UpdatedAt = DateTime.UtcNow;

            AddActivity(new TicketActivity(
                    Id,
                    TicketActivityType.Assigned,
                    assignerId,
                    details));
        }

        /// <summary>
        /// Removes the current support agent assignment from the ticket.
        /// </summary>
        public void Unassign()
        {
            AssignedToId = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Changes the priority of the ticket.
        /// </summary>
        /// <param name="priority">The new priority level.</param>
        /// <param name="agentId">The unique identifier of the user making the change.</param>
        public void ChangePriority(TicketPriority priority, Guid agentId)
        {
            if (Priority == priority)
                return;

            var previousPriority = Priority;

            Priority = priority;
            UpdatedAt = DateTime.UtcNow;

            AddActivity(new TicketActivity(
                Id,
                TicketActivityType.PriorityChanged,
                agentId,
                $"Ticket priority changed from {previousPriority} to {priority}."));
        }

        /// <summary>
        /// Moves an open ticket into the InProgress status.
        /// </summary>
        /// <exception cref="DomainException">
        /// Thrown when the ticket is not currently open.
        /// </exception>
        public void StartProgress()
        {
            if (Status != TicketStatus.Open)
                throw new DomainException("Only open tickets can be moved into progress.");

            Status = TicketStatus.InProgress;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a public message to the ticket.
        /// </summary>
        /// <param name="content">The content of the message.</param>
        /// <param name="authorId">The unique identifier of the user authoring the message.</param>
        /// <exception cref="DomainException">
        /// Thrown when the author identifier is invalid or the author is neither the ticket creator nor its currently
        /// assigned support agent.
        /// </exception>
        public void AddPublicMessage(string content, Guid authorId)
        {
            if (authorId == Guid.Empty)
                throw new DomainException("A valid user ID must be specified to add a public message");

            if (authorId != CreatedById && authorId != AssignedToId)
                throw new DomainException("A public message can not be added, unless the user is the author of the ticket, or the assigned support agent.");

            var message = new TicketMessage(Id, content, authorId);
            AddMessage(message);

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds an internal note to the ticket.
        /// </summary>
        /// <param name="content">The content of the internal note.</param>
        /// <param name="authorId">The unique identifier of the user authoring the note.</param>
        /// <param name="isAdministrator">Whether the author is an administrator.</param>
        /// <exception cref="DomainException">
        /// Thrown when the author identifier is invalid or the author is not the currently assigned support 
        /// agent or an administrator.
        /// </exception>
        /// <remarks>
        /// Used by support agents and administrators to add internal notes to ticket, not visible to the customer. 
        /// </remarks>
        public void AddInternalNote(string content, Guid authorId, bool isAdministrator = false)
        {
            if (authorId == Guid.Empty)
                throw new DomainException("A valid user ID must be specified to add a public message");

            if (!isAdministrator && authorId != AssignedToId)
                throw new DomainException("Only the assigned support agent or an administrator can add an internal note.");

            var message = new TicketMessage(Id, content, authorId, isPublic: false);
            AddMessage(message);

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the ticket as resolved.
        /// </summary>
        /// <exception cref="DomainException">
        /// Thrown when the ticket is not open or in progress.
        /// </exception>
        public void Resolve()
        {
            if (Status is not TicketStatus.Open and not TicketStatus.InProgress)
                throw new DomainException("Only open or in-progress tickets can be resolved.");

            Status = TicketStatus.Resolved;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Closes a resolved ticket.
        /// </summary>
        /// <exception cref="DomainException">
        /// Thrown when the ticket has not been resolved.
        /// </exception>
        public void Close()
        {
            if (Status != TicketStatus.Resolved)
                throw new DomainException("Only resolved tickets can be closed.");

            Status = TicketStatus.Closed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Reopens a resolved or closed ticket, returning it to the Open status.
        /// </summary>
        /// <exception cref="DomainException">
        /// Thrown when the ticket is neither resolved nor closed.
        /// </exception>
        public void Reopen()
        {
            if (Status is not TicketStatus.Resolved and not TicketStatus.Closed)
                throw new DomainException("Only resolved or closed tickets can be reopened.");

            Status = TicketStatus.Open;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds an escalation request to the ticket.
        /// </summary>
        /// <remarks>
        /// Only the currently assigned support agent can request an escalation. A ticket cannot have more than one pending 
        /// escalation request. 
        /// </remarks>
        /// <param name="escalationRequest">The escalation request being submitted.</param>
        /// <param name="requesterDisplayName">The display name of the agent requesting the escalation.</param>
        /// <param name="proposedRecipientDisplayName">The display name of the proposed recipient, if one is specified.</param>
        /// <exception cref="DomainException">
        /// Thrown when the ticket is closed, the requester is not the assigned agent, another escalation is pending, or the
        /// supplied details are invalid.
        /// </exception>
        public void RequestEscalation(
            EscalationRequest escalationRequest, 
            string requesterDisplayName, 
            string? proposedRecipientDisplayName)
        {
            if (escalationRequest is null)
                throw new DomainException("An escalation request must be specified.");

            if (Status == TicketStatus.Closed)
                throw new DomainException("Cannot escalate a closed ticket.");

            if (AssignedToId != escalationRequest.RequestedByUserId)
                throw new DomainException("Only the assigned support agent can request an escalation.");

            if (_escalationRequests.Any(e => e.Status == EscalationRequestStatus.Waiting))
                throw new DomainException("This ticket already has a pending escalation request.");

            if (string.IsNullOrWhiteSpace(requesterDisplayName))
                throw new DomainException("A valid requester display name must be specified.");

            if (escalationRequest.ProposedRecipientUserId.HasValue && string.IsNullOrWhiteSpace(proposedRecipientDisplayName))
                throw new DomainException("A proposed recipient display name must be specified.");

            _escalationRequests.Add(escalationRequest);

            string details = proposedRecipientDisplayName != null
                ? $"{requesterDisplayName} requested an escalation to {proposedRecipientDisplayName}."
                : $"{requesterDisplayName} requested an escalation.";


            AddActivity(new TicketActivity(
                Id,
                TicketActivityType.EscalationRequested,
                escalationRequest.RequestedByUserId,
                details: details,
                isPublic: false));
        }

        /// <summary>
        /// Accepts an escalation request and reassigns the ticket to another support agent.
        /// </summary>
        /// <remarks>
        /// The escalation request must belong to this ticket and still be pending. The ticket must remain assigned to the 
        /// agent who requested the escalation. 
        /// </remarks>
        /// <param name="escalationRequest">The escalation request being accepted.</param>
        /// <param name="administratorId">The unique identifier of the administrator accepting the request.</param>
        /// <param name="administratorDisplayName">The display name of the administrator.</param>
        /// <param name="newSupportAgentId">The unique identifier of the support agent receiving the ticket.</param>
        /// <param name="newSupportAgentDisplayName">The display name of the new support agent.</param>
        /// <param name="previousSupportAgentDisplayName">The display name of the previously assigned support agent.</param>
        /// <param name="responseReason">An optional explanation explaining the administrator's decision.</param>
        /// <exception cref="DomainException">
        /// Thrown when the request is invalid, does not belong to this ticket, has already been responded to, or 
        /// the ticket is no longer eligible for escalation.
        /// </exception>
        public void AcceptEscalation(
            EscalationRequest escalationRequest, 
            Guid administratorId, 
            string administratorDisplayName,
            Guid newSupportAgentId, 
            string newSupportAgentDisplayName, 
            string previousSupportAgentDisplayName, 
            string? responseReason)
        {
            if (escalationRequest == null)
                throw new DomainException("An escalation request must be specified.");

            if (escalationRequest.TicketId != Id)
                throw new DomainException("The escalation request does not belong to this ticket.");

            if (escalationRequest.Status != EscalationRequestStatus.Waiting)
                throw new DomainException("This escalation request has already been responded to.");

            if (Status == TicketStatus.Closed)
                throw new DomainException("Cannot accept an escalation for a closed ticket.");

            if (AssignedToId != escalationRequest.RequestedByUserId)
                throw new DomainException("The ticket is no longer assigned to the agent who requested escalation.");

            if (administratorId == Guid.Empty)
                throw new DomainException("The administrator must be specified.");

            if (newSupportAgentId == Guid.Empty)
                throw new DomainException("The new support agent must be specified.");

            if (string.IsNullOrWhiteSpace(administratorDisplayName))
                throw new DomainException("The administrator's display name is required.");

            if (string.IsNullOrWhiteSpace(newSupportAgentDisplayName))
                throw new DomainException("The new support agent's display name is required.");

            escalationRequest.Accept(
                administratorId,
                newSupportAgentId,
                responseReason);

            AssignedToId = newSupportAgentId;
            UpdatedAt = DateTime.UtcNow;

            
            AddActivity(new TicketActivity(
                Id,
                TicketActivityType.EscalationAccepted,
                administratorId,
                details: $"{administratorDisplayName} accepted the escalation request and reassigned the ticket from {previousSupportAgentDisplayName} to {newSupportAgentDisplayName}.",
                isPublic: false));
        }

        /// <summary>
        /// Rejects an escalation request without changing the ticket's assignment.
        /// </summary>
        /// <param name="escalationRequest">The escalation request being rejected.</param>
        /// <param name="administratorId">The unique identifier of the administrator rejecting the request.</param>
        /// <param name="administratorDisplayName">The display name of the administrator.</param>
        /// <param name="responseReason">An optional explanation accompanying the administrator's decision.</param>
        /// <exception cref="DomainException">
        /// Thrown when the request is invalid, does not belong to this ticket, has already been responded to, or the 
        /// ticket is no longer eligible for escalation.
        /// </exception>
        public void RejectEscalation(
            EscalationRequest escalationRequest,
            Guid administratorId,
            string administratorDisplayName,
            string? responseReason)
        {
            if (escalationRequest == null)
                throw new DomainException("An escalation request must be specified.");

            if (escalationRequest.TicketId != Id)
                throw new DomainException("The escalation request does not belong to this ticket.");

            if (escalationRequest.Status != EscalationRequestStatus.Waiting)
                throw new DomainException("This escalation request has already been responded to.");

            if (Status == TicketStatus.Closed)
                throw new DomainException("Cannot reject an escalation for a closed ticket.");

            if (AssignedToId != escalationRequest.RequestedByUserId)
                throw new DomainException("The ticket is no longer assigned to the agent who requested escalation.");

            if (administratorId == Guid.Empty)
                throw new DomainException("The administrator must be specified.");

            if (string.IsNullOrWhiteSpace(administratorDisplayName))
                throw new DomainException("The administrator's display name is required.");

            escalationRequest.Reject(
                administratorId,
                responseReason);

            UpdatedAt = DateTime.UtcNow;

            AddActivity(new TicketActivity(
                Id,
                TicketActivityType.EscalationRejected,
                administratorId,
                details: $"{administratorDisplayName} rejected the escalation request.",
                isPublic: false));
        }
    }
}
