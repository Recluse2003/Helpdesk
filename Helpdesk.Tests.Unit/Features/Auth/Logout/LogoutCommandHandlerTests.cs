using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Features.Auth.Logout;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Auth.Logout
{
    public class LogoutCommandHandlerTests : IDisposable
    {
        private readonly HelpdeskDbContext _context;
        private readonly Mock<ITokenService> _tokenService;
        private readonly LogoutCommandHandler _handler;

        private readonly SqliteConnection _connection;

        public LogoutCommandHandlerTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new HelpdeskDbContext(options);
            _context.Database.EnsureCreated();

            _tokenService = new Mock<ITokenService>();

            _handler = new LogoutCommandHandler(
                _context,
                _tokenService.Object);
        }

        [Fact]
        public async Task Handle_WithActiveRefreshToken_ShouldRevokeToken()
        {
            // Arrange
            var rawToken = "refresh-token";
            var tokenHash = "hashed-refresh-token";
            var userId = Guid.NewGuid();

            var refreshToken = new RefreshToken(
                userId,
                tokenHash,
                DateTime.UtcNow.AddDays(7));

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            _tokenService
                .Setup(x => x.HashRefreshToken(rawToken))
                .Returns(tokenHash);

            var command = new LogoutCommand(rawToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var storedToken = await _context.RefreshTokens
                .AsNoTracking()
                .SingleAsync();

            storedToken.IsRevoked.Should().BeTrue();
            storedToken.RevokedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithUnknownRefreshToken_ShouldSucceed()
        {
            // Arrange
            _tokenService
                .Setup(x => x.HashRefreshToken("unknown-token"))
                .Returns("unknown-hash");

            var command = new LogoutCommand("unknown-token");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            (await _context.RefreshTokens.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithExpiredRefreshToken_ShouldNotRevokeToken()
        {
            // Arrange
            var rawToken = "expired-token";
            var tokenHash = "expired-hash";

            var refreshToken = new RefreshToken(
                Guid.NewGuid(),
                tokenHash,
                DateTime.UtcNow.AddSeconds(0.5));

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            _tokenService
                .Setup(x => x.HashRefreshToken(rawToken))
                .Returns(tokenHash);

            var command = new LogoutCommand(rawToken);

            Thread.Sleep(1000);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var storedToken = await _context.RefreshTokens
                .SingleAsync();

            storedToken.IsRevoked.Should().BeFalse();
            storedToken.RevokedAt.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithAlreadyRevokedRefreshToken_ShouldSucceed()
        {
            // Arrange
            var rawToken = "revoked-token";
            var tokenHash = "revoked-hash";

            var refreshToken = new RefreshToken(
                Guid.NewGuid(),
                tokenHash,
                DateTime.UtcNow.AddDays(7));

            refreshToken.Revoke();

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            _tokenService
                .Setup(x => x.HashRefreshToken(rawToken))
                .Returns(tokenHash);

            var command = new LogoutCommand(rawToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var storedToken = await _context.RefreshTokens
                .SingleAsync();

            storedToken.IsRevoked.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldOnlyRevokeMatchingRefreshToken()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var matchingToken = new RefreshToken(
                userId,
                "matching-hash",
                DateTime.UtcNow.AddDays(7));

            var otherToken = new RefreshToken(
                userId,
                "other-hash",
                DateTime.UtcNow.AddDays(7));

            _context.RefreshTokens.AddRange(
                matchingToken,
                otherToken);

            await _context.SaveChangesAsync();

            _tokenService
                .Setup(x => x.HashRefreshToken("raw-token"))
                .Returns("matching-hash");

            var command = new LogoutCommand("raw-token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var storedTokens = await _context.RefreshTokens
                .AsNoTracking()
                .OrderBy(x => x.TokenHash)
                .ToListAsync();

            storedTokens.Single(x => x.TokenHash == "matching-hash")
                .IsRevoked.Should().BeTrue();

            storedTokens.Single(x => x.TokenHash == "other-hash")
                .IsRevoked.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
