using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Api.Domain.Entities
{
    public class Ticket
    {
        public Guid Id { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public TicketStatus Status { get; private set; }

        public TicketPriority Priority { get; private set; }

        public Guid CreatedById { get; private set; }

        public Guid? AssignedToId { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        private readonly List<TicketMessage> _messages = [];
        private readonly List<TicketActivity> _activities = [];

        public IReadOnlyCollection<TicketMessage> Messages => _messages.AsReadOnly();
        public IReadOnlyCollection<TicketActivity> Activities => _activities.AsReadOnly();

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

        private void AddMessage(TicketMessage message)
        {
            _messages.Add(message);
        }

        private void AddActivity(TicketActivity activity)
        {
            _activities.Add(activity);
        }

        public void AssignTo(Guid agentId)
        {
            if (agentId == Guid.Empty)
                throw new DomainException("A valid user ID must be specified to assign the ticket.");

            AssignedToId = agentId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Unassign()
        {
            AssignedToId = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangePriority(TicketPriority priority, Guid agentId)
        {
            var activity = new TicketActivity(Id, TicketActivityType.PriorityChanged, agentId, $"Agent has reassigned ticket priority from {Priority} to {priority}.");
            AddActivity(activity);

            Priority = priority;
            UpdatedAt = DateTime.UtcNow;
        }

        public void StartProgress()
        {
            if (Status != TicketStatus.Open)
                throw new DomainException("Only open tickets can be moved into progress.");

            Status = TicketStatus.InProgress;
            UpdatedAt = DateTime.UtcNow;
        }

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

        public void AddInternalNote(string content, Guid authorId)
        {
            if (authorId == Guid.Empty)
                throw new DomainException("A valid user ID must be specified to add a public message");

            if (authorId != AssignedToId)
                throw new DomainException("A internal note can not be added to a ticket, unless they are the assigned support agent.");

            var message = new TicketMessage(Id, content, authorId, isPublic: false);
            AddMessage(message);

            UpdatedAt = DateTime.UtcNow;
        }

        public void Resolve()
        {
            if (Status is not TicketStatus.Open and not TicketStatus.InProgress)
                throw new DomainException("Only open or in-progress tickets can be resolved.");

            Status = TicketStatus.Resolved;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Close()
        {
            if (Status != TicketStatus.Resolved)
                throw new DomainException("Only resolved tickets can be closed.");

            Status = TicketStatus.Closed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reopen()
        {
            if (Status is not TicketStatus.Resolved and not TicketStatus.Closed)
                throw new DomainException("Only resolved or closed tickets can be reopened.");

            Status = TicketStatus.Open;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
