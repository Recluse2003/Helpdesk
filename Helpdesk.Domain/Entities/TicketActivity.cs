using Helpdesk.Domain.Enums;

namespace Helpdesk.Domain.Entities
{
    public class TicketActivity
    {
        public Guid Id { get; private set; }

        public TicketActivityType EventType { get; private set; }

        public bool IsPublic { get; private set; }

        public string? Details { get; private set; }

        public Guid TicketId { get; private set; }

        public Guid? ActorUserId { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public TicketActivity(Guid ticketId, TicketActivityType eventType, Guid actorUserId, string? details = null, bool isPublic = true)
        {
            if (details != null && details.Length > 1000)
                throw new ArgumentException("A ticket activity's details cannot exceed 1000 characters.", nameof(details));

            if (ticketId != Guid.Empty)
                throw new ArgumentException("A ticket must be specified.", nameof(ticketId));

            if (actorUserId != Guid.Empty)
                throw new ArgumentException("A ticket activity acting user must be specified.", nameof(ticketId));

            Id = Guid.NewGuid();
            EventType = eventType;
            IsPublic = isPublic;
            Details = details;
            TicketId = ticketId;
            ActorUserId = actorUserId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
