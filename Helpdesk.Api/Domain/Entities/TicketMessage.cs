using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Api.Domain.Entities
{
    /// <summary>
    /// Represents a message or internal note for a specified support ticket. 
    /// </summary>
    /// <remarks>
    /// A message is authored by a user and can either be publicly visible to the customer or restricted to support staff.
    /// </remarks>
    public class TicketMessage
    {
        /// <summary>
        /// Gets the unique identifier of the message.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the content of the message.
        /// </summary>
        public string Content { get; private set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the message is visible to the customer.
        /// </summary>
        public bool IsPublic { get; private set; }

        /// <summary>
        /// Gets the identifier of the ticket associated with the message.
        /// </summary>
        public Guid TicketId { get; private set; }

        /// <summary>
        /// Gets the identifier of the user who authored the message.
        /// </summary>
        public Guid AuthorUserId { get; private set; }

        /// <summary>
        /// Gets the date and time when the message was created, in UTC.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Creates a new message or internal note associated with a specified support ticket.
        /// </summary>
        /// <param name="ticketId">The unique identifier of the ticket the message is for.</param>
        /// <param name="content">The content of the message.</param>
        /// <param name="authorUserId">The unique identifier of the user creating the message.</param>
        /// <param name="isPublic">Indicates whether the message is visible to the customer. Defaults to <see langword="true"/>.</param>
        /// <exception cref="DomainException">
        /// Thrown when the message content is empty, exceeds the permitted length, or the ticket or author identifier 
        /// is invalid.
        /// </exception>
        public TicketMessage(Guid ticketId, string content, Guid authorUserId, bool isPublic = true)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new DomainException("A ticket message must contain something.");

            if (content.Length > 5000)
                throw new DomainException("A ticket message cannot exceed 5000 characters.");

            if (ticketId == Guid.Empty)
                throw new DomainException("A ticket must be specified.");

            if (authorUserId == Guid.Empty)
                throw new DomainException("The message's author must be specified.");

            Id = Guid.NewGuid();
            Content = content.Trim();
            IsPublic = isPublic;
            TicketId = ticketId;
            AuthorUserId = authorUserId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
