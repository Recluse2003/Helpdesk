namespace Helpdesk.Domain.Entities
{
    public class TicketActivity
    {
        public Guid Id { get; private set; }

        public string EventType { get; private set; } = string.Empty;

        public bool IsPublic { get; private set; }

        public string? Details { get; private set; }

        public Guid TicketId { get; private set; }

        public Guid ActorUserId { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public TicketActivity(Guid id, string eventType, bool isPublic, string? details, Guid ticketId, Guid actorUserId)
        {
            Id = id;
            EventType = eventType;
            IsPublic = isPublic;
            Details = details;
            TicketId = ticketId;
            ActorUserId = actorUserId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
