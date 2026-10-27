using Helpdesk.Domain.Enums;
using Helpdesk.Domain.Exceptions;

namespace Helpdesk.Domain.Entities
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
        }

        public void AssignTo(Guid agentId)
        {
            if (agentId == Guid.Empty)
                throw new ArgumentException("A valid user ID must be specified.", nameof(agentId));

            AssignedToId = agentId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Unassign()
        {
            AssignedToId = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangePriority(TicketPriority priority)
        {
            Priority = priority;
            UpdatedAt = DateTime.UtcNow;
        }

        public void StartProgress()
        {
            if (Status != TicketStatus.Open)
                throw new InvalidOperationException("Only open tickets can be moved into progress.");

            Status = TicketStatus.InProgress;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Resolve()
        {
            if (Status is not TicketStatus.Open and not TicketStatus.InProgress)
                throw new InvalidOperationException("Only open or in-progress tickets can be resolved.");

            Status = TicketStatus.Resolved;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Close()
        {
            if (Status != TicketStatus.Resolved)
                throw new InvalidOperationException("Only resolved tickets can be closed.");

            Status = TicketStatus.Closed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reopen()
        {
            if (Status is not TicketStatus.Resolved and not TicketStatus.Closed)
                throw new InvalidOperationException("Only resolved or closed tickets can be reopened.");

            Status = TicketStatus.Open;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
