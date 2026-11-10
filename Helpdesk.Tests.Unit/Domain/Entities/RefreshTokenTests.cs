using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Tests.Unit.Domain.Entities
{
    public class RefreshTokenTests
    {
        [Fact]
        public void Constructor_WithValidDetails_ShouldCreateRefreshToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenHash = "hashed-refresh-token";
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            var token = new RefreshToken(userId, tokenHash, expiresAt);

            // Assert
            token.Id.Should().NotBeEmpty();
            token.UserId.Should().Be(userId);
            token.TokenHash.Should().Be(tokenHash);
            token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            token.ExpiresAt.Should().Be(expiresAt);
            token.RevokedAt.Should().BeNull();
            token.ReplacedByTokenHash.Should().BeNull();
            token.IsRevoked.Should().BeFalse();
            token.IsExpired.Should().BeFalse();
            token.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithEmptyUserId_ShouldThrowDomainException()
        {
            // Arrange
            var tokenHash = "hashed-refresh-token";
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            Action act = () => new RefreshToken(
                Guid.Empty,
                tokenHash,
                expiresAt);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A user must be specified.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Constructor_WithMissingTokenHash_ShouldThrowDomainException(string tokenHash)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            Action act = () => new RefreshToken(
                userId,
                tokenHash,
                expiresAt);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A token hash must be provided.");
        }

        [Fact]
        public void Constructor_WithExpiredToken_ShouldThrowDomainException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenHash = "hashed-refresh-token";
            var expiresAt = DateTime.UtcNow.AddDays(-1);

            // Act
            Action act = () => new RefreshToken(
                userId,
                tokenHash,
                expiresAt);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("The token expiry must be in the future.");
        }

        [Fact]
        public void Constructor_WithExpiryEqualToCurrentTime_ShouldThrowDomainException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenHash = "hashed-refresh-token";
            var expiresAt = DateTime.UtcNow;

            // Act
            Action act = () => new RefreshToken(
                userId,
                tokenHash,
                expiresAt);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("The token expiry must be in the future.");
        }

        [Fact]
        public void Revoke_ShouldMarkTokenAsRevoked()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddDays(7));

            // Act
            token.Revoke();

            // Assert
            token.IsRevoked.Should().BeTrue();
            token.RevokedAt.Should().NotBeNull();
            token.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            token.ReplacedByTokenHash.Should().BeNull();
            token.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Revoke_WithReplacementHash_ShouldStoreReplacementHash()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                "original-hash",
                DateTime.UtcNow.AddDays(7));

            var replacementHash = "replacement-hash";

            // Act
            token.Revoke(replacementHash);

            // Assert
            token.IsRevoked.Should().BeTrue();
            token.ReplacedByTokenHash.Should().Be(replacementHash);
        }

        [Fact]
        public void Revoke_WhenAlreadyRevoked_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddDays(7));

            token.Revoke();

            // Act
            Action act = () => token.Revoke();

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("Refresh token has already been revoked.");
        }

        [Fact]
        public void IsExpired_WhenExpiryDateIsInThePast_ShouldReturnTrue()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddSeconds(1));

            Thread.Sleep(1000);

            // Act & Assert
            token.IsExpired.Should().BeTrue();
        }

        [Fact]
        public void IsExpired_WhenExpiryDateIsInTheFuture_ShouldReturnFalse()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddDays(7));

            // Act & Assert
            token.IsExpired.Should().BeFalse();
        }

        [Fact]
        public void IsActive_WhenTokenIsNotRevokedOrExpired_ShouldReturnTrue()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddDays(7));

            // Act & Assert
            token.IsActive.Should().BeTrue();
        }

        [Fact]
        public void IsActive_WhenTokenIsRevoked_ShouldReturnFalse()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddDays(7));

            token.Revoke();

            // Act & Assert
            token.IsActive.Should().BeFalse();
        }

        [Fact]
        public void IsActive_WhenTokenIsExpired_ShouldReturnFalse()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddSeconds(1));

            Thread.Sleep(1000);

            // Act & Assert
            token.IsActive.Should().BeFalse();
        }

        [Fact]
        public void IsActive_WhenTokenIsBothRevokedAndExpired_ShouldReturnFalse()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddSeconds(1));

            Thread.Sleep(1000);

            token.Revoke();

            // Act & Assert
            token.IsRevoked.Should().BeTrue();
            token.IsExpired.Should().BeTrue();
            token.IsActive.Should().BeFalse();
        }
    }
}
