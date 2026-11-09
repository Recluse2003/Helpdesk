namespace Helpdesk.Api.Domain.Entities
{
    /// <summary>
    /// Represents a refresh token issued to a specified user for maintaining an authenticated session.
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        /// Gets the unique identifier of the refresh token record.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the identifier of the user associated with this token.
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Gets the SHA-256 hash of the refresh token.
        /// </summary>
        public string TokenHash { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the date and time the refresh token was created.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets the date and time after which the refresh token expires.
        /// </summary>
        public DateTime ExpiresAt { get; private set; }

        /// <summary>
        /// Gets the date and time the refresh token was revoked, if applicable.
        /// </summary>
        public DateTime? RevokedAt { get; private set; }

        /// <summary>
        /// Gets the hash of the replacement refresh token, if this token
        /// was revoked as part of token rotation.
        /// </summary>
        public string? ReplacedByTokenHash { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the refresh token has been revoked.
        /// </summary>
        public bool IsRevoked => RevokedAt.HasValue;

        /// <summary>
        /// Gets a value indicating whether the refresh token has expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// Gets a value indicating whether the refresh token is neither
        /// revoked nor expired.
        /// </summary>
        public bool IsActive => !IsRevoked && !IsExpired;

        private RefreshToken() { }

        /// <summary>
        /// Creates a new refresh token.
        /// </summary>
        /// <param name="userId">The unique identifier of the user associated with the token.</param>
        /// <param name="tokenHash">The hashed representation of the refresh token.</param>
        /// <param name="expiresAt">The date and time the token expires.</param>
        public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            TokenHash = tokenHash;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = expiresAt;
        }

        /// <summary>
        /// Revokes the refresh token and optionally records its  replacement.
        /// </summary>
        /// <param name="replacedByTokenHash">The hash of the replacement refresh token, if the token is being rotated.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the refresh token has already been revoked.
        /// </exception>
        public void Revoke(string? replacedByTokenHash = null)
        {
            if (RevokedAt != null)
                throw new InvalidOperationException("Refresh token has already been revoked.");

            RevokedAt = DateTime.UtcNow;
            ReplacedByTokenHash = replacedByTokenHash;
        }
    }
}
