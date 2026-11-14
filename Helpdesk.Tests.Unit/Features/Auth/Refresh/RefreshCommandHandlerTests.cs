using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Features.Auth.Login;
using Helpdesk.Api.Features.Auth.Refresh;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Auth.Refresh
{
    public class RefreshCommandHandlerTests
    {
        private static async Task<TestContext> CreateContextAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new HelpdeskDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            var tokenService = new Mock<ITokenService>();
            tokenService
                .Setup(x => x.HashRefreshToken(It.IsAny<string>()))
                .Returns<string>(token => $"hash-{token}");

            tokenService
                .Setup(x => x.GenerateAccessToken(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>()))
                .Returns("new-access-token");

            tokenService
                .Setup(x => x.GenerateRefreshToken())
                .Returns("new-refresh-token");

            var userManager = IdentityMockFactory.CreateUserManager();

            return new TestContext(connection, dbContext, tokenService, userManager);
        }

        private static ApplicationUser CreateUser()
        {
            return new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"user-{Guid.NewGuid()}@example.com",
                Email = $"user-{Guid.NewGuid()}@example.com",
                DisplayName = "Test user",
                EmailConfirmed = true
            };
        }

        private static async Task<RefreshToken> AddStoredTokenAsync(TestContext context, ApplicationUser user)
        {
            var token = new RefreshToken(
                user.Id,
                "hash-valid-token",
                DateTime.UtcNow.AddDays(7));

            context.DbContext.Users.Add(user);
            context.DbContext.RefreshTokens.Add(token);

            await context.DbContext.SaveChangesAsync();

            return token;
        }

        private static RefreshCommandHandler CreateHandler(TestContext context)
        {
            return new RefreshCommandHandler(
                context.DbContext,
                context.UserManager.Object,
                context.TokenService.Object);
        }

        private sealed class TestContext : IAsyncDisposable
        {
            public SqliteConnection Connection { get; }
            public HelpdeskDbContext DbContext { get; }
            public Mock<ITokenService> TokenService { get; }
            public Mock<UserManager<ApplicationUser>> UserManager { get; }

            public TestContext(
                SqliteConnection connection,
                HelpdeskDbContext dbContext,
                Mock<ITokenService> tokenService,
                Mock<UserManager<ApplicationUser>> userManager)
            {
                Connection = connection;
                DbContext = dbContext;
                TokenService = tokenService;
                UserManager = userManager;
            }

            public async ValueTask DisposeAsync()
            {
                await DbContext.DisposeAsync();
                await Connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task Handle_WhenRefreshTokenDoesNotExist_ReturnsUnauthorized()
        {
            await using var context = await CreateContextAsync();

            var handler = CreateHandler(context);

            var result = await handler.Handle(
                new RefreshCommand("unknown-token"),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsUnauthorized()
        {
            await using var context = await CreateContextAsync();

            var userId = Guid.NewGuid();
            var storedToken = new RefreshToken(
                userId,
                "hash-valid-token",
                DateTime.UtcNow.AddDays(7));

            context.DbContext.RefreshTokens.Add(storedToken);
            await context.DbContext.SaveChangesAsync();

            context.UserManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            var handler = CreateHandler(context);

            var result = await handler.Handle(
                new RefreshCommand("valid-token"),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenEmailIsNotConfirmed_ReturnsUnauthorized()
        {
            await using var context = await CreateContextAsync();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "customer@example.com",
                UserName = "customer@example.com",
                DisplayName = "Customer",
                EmailConfirmed = false
            };

            var storedToken = new RefreshToken(
                user.Id,
                "hash-valid-token",
                DateTime.UtcNow.AddDays(7));

            context.DbContext.Users.Add(user);
            context.DbContext.RefreshTokens.Add(storedToken);
            await context.DbContext.SaveChangesAsync();

            context.UserManager
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            var handler = CreateHandler(context);

            var result = await handler.Handle(
                new RefreshCommand("valid-token"),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenUserIsLockedOut_ReturnsUnauthorized()
        {
            await using var context = await CreateContextAsync();

            var user = CreateUser();

            await AddStoredTokenAsync(context, user);

            context.UserManager
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            context.UserManager
                .Setup(x => x.IsLockedOutAsync(user))
                .ReturnsAsync(true);

            var handler = CreateHandler(context);

            var result = await handler.Handle(
                new RefreshCommand("valid-token"),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenTokenIsValid_RotatesTokenAndReturnsNewTokens()
        {
            await using var context = await CreateContextAsync();

            var user = CreateUser();
            var storedToken = await AddStoredTokenAsync(context, user);

            context.UserManager
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            context.UserManager
                .Setup(x => x.IsLockedOutAsync(user))
                .ReturnsAsync(false);

            context.UserManager
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Customer" });

            var handler = CreateHandler(context);

            var result = await handler.Handle(
                new RefreshCommand("valid-token"),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("new-access-token", result.Value.AccessToken);
            Assert.Equal("new-refresh-token", result.Value.RefreshToken);

            var oldToken = await context.DbContext.RefreshTokens
                .AsNoTracking()
                .SingleAsync(x => x.Id == storedToken.Id);

            Assert.True(oldToken.IsRevoked);
            Assert.Equal("hash-new-refresh-token", oldToken.ReplacedByTokenHash);

            var replacement = await context.DbContext.RefreshTokens
                .SingleOrDefaultAsync(x => x.TokenHash == "hash-new-refresh-token");

            Assert.NotNull(replacement);
            Assert.True(replacement.IsActive);
            Assert.Equal(user.Id, replacement.UserId);
        }

        [Fact]
        public async Task Handle_WhenOldTokenIsReused_ReturnsUnauthorized()
        {
            await using var context = await CreateContextAsync();

            var user = CreateUser();
            var storedToken = await AddStoredTokenAsync(context, user);

            context.UserManager
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            context.UserManager
                .Setup(x => x.IsLockedOutAsync(user))
                .ReturnsAsync(false);

            context.UserManager
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Customer" });

            var handler = CreateHandler(context);

            // First refresh should succeed.
            var firstResult = await handler.Handle(
                new RefreshCommand("valid-token"),
                CancellationToken.None);

            Assert.True(firstResult.IsSuccess);

            // The original token has now been revoked.
            var secondResult = await handler.Handle(
                new RefreshCommand("valid-token"),
                CancellationToken.None);

            Assert.False(secondResult.IsSuccess);
        }

 
    }
}
