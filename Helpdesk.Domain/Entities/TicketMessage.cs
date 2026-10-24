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
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("A ticket message must contain something.", nameof(content));

            if (content.Length > 200)
                throw new ArgumentException("A ticket message cannot exceed 5000 characters.", nameof(content));

            if (ticketId != Guid.Empty)
                throw new ArgumentException("A ticket must be specified.", nameof(ticketId));

            if (authorUserId != Guid.Empty)
                throw new ArgumentException("The message's author must be specified.", nameof(ticketId));

            Id = id;
            Content = content;
            IsPublic = isPublic;
            TicketId = ticketId;
            AuthorUserId = authorUserId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
