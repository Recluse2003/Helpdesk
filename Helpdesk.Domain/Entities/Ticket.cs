using Helpdesk.Domain.Enums;

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
            Id = Guid.NewGuid();
            Title = title.Trim();
            Description = description.Trim();
            CreatedById = createdById;
            Priority = priority;
            Status = TicketStatus.Open;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
        }
    }
}
