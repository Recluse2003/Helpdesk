namespace Helpdesk.Domain.Entities
{
    public class TicketMessage
    {
        public Guid Id { get; private set; }

        public string Content { get; private set; } = string.Empty;

        public bool IsPublic { get; private set; }

        public Guid TicketId { get; private set; }

        public Guid AuthorUserId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public TicketMessage(Guid id, string content, bool isPublic, Guid ticketId, Guid authorUserId)
        {
            Id = id;
            Content = content;
            IsPublic = isPublic;
            TicketId = ticketId;
            AuthorUserId = authorUserId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
