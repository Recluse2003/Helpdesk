using FluentAssertions;
using Helpdesk.Api.Features.Auth.Login;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Auth.Login
{
    public class LoginCommandHandlerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManager;
        private readonly Mock<ITokenService> _tokenService;
        private readonly HelpdeskDbContext _context;
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _userManager = IdentityMockFactory.CreateUserManager();
            _signInManager = IdentityMockFactory.CreateSignInManager();
            _tokenService = new Mock<ITokenService>();

            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);

            _handler = new LoginCommandHandler(
                _userManager.Object,
                _signInManager.Object,
                _tokenService.Object,
                _context);
        }

        [Fact]
        public async Task Handle_WithValidCredentials_ShouldReturnTokensAndPersistRefreshToken()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                UserName = "test@test.com",
                DisplayName = "Test User",
                EmailConfirmed = true
            };

            var command = new LoginCommand("test@test.com", "Password123!");

            _userManager
                .Setup(x => x.FindByEmailAsync("test@test.com"))
                .ReturnsAsync(user);

            _signInManager
                .Setup(x => x.CheckPasswordSignInAsync(user, command.Password, true))
                .ReturnsAsync(SignInResult.Success);

            _userManager
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Customer" });

            _tokenService
                .Setup(x => x.GenerateAccessToken(user.Id, user.Email!, user.DisplayName, It.IsAny<IEnumerable<string>>()))
                .Returns("access-token");

            _tokenService
                .Setup(x => x.GenerateRefreshToken())
                .Returns("refresh-token");

            _tokenService
                .Setup(x => x.HashRefreshToken("refresh-token"))
                .Returns("hashed-refresh-token");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.AccessToken.Should().Be("access-token");
            result.Value.RefreshToken.Should().Be("refresh-token");

            var storedToken = await _context.RefreshTokens.SingleAsync();

            storedToken.UserId.Should().Be(user.Id);
            storedToken.TokenHash.Should().Be("hashed-refresh-token");
            storedToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));

            _tokenService.Verify(
                x => x.GenerateAccessToken(user.Id, user.Email!, user.DisplayName, It.IsAny<IEnumerable<string>>()),
                Times.Once);

            _tokenService.Verify(
                x => x.GenerateRefreshToken(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithUnknownEmail_ShouldReturnUnauthorized()
        {
            // Arrange
            _userManager
                .Setup(x => x.FindByEmailAsync("unknown@test.com"))
                .ReturnsAsync((ApplicationUser?)null);

            var command = new LoginCommand("unknown@test.com", "Password123!");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.InvalidCredentials");

            _signInManager.Verify(
                x => x.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);

            _tokenService.Verify(
                x => x.GenerateRefreshToken(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithUnconfirmedEmail_ShouldReturnConflict()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                UserName = "test@test.com",
                DisplayName = "Test User",
                EmailConfirmed = false
            };

            _userManager
                .Setup(x => x.FindByEmailAsync("test@test.com"))
                .ReturnsAsync(user);

            var command = new LoginCommand("test@test.com", "Password123!");

            // Act
            var result = await _handler.Handle(
                command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.EmailNotConfirmed");

            _signInManager.Verify(
                x => x.CheckPasswordSignInAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithInvalidPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                UserName = "test@test.com",
                DisplayName = "Test User",
                EmailConfirmed = true
            };

            _userManager
                .Setup(x => x.FindByEmailAsync("test@test.com"))
                .ReturnsAsync(user);

            _signInManager
                .Setup(x => x.CheckPasswordSignInAsync(user, "WrongPassword", true))
                .ReturnsAsync(SignInResult.Failed);

            var command = new LoginCommand("test@test.com", "WrongPassword");

            // Act
            var result = await _handler.Handle(
                command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.InvalidCredentials");

            _tokenService.Verify(
                x => x.GenerateRefreshToken(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithLockedOutUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                UserName = "test@test.com",
                DisplayName = "Test User",
                EmailConfirmed = true
            };

            _userManager
                .Setup(x => x.FindByEmailAsync("test@test.com"))
                .ReturnsAsync(user);

            _signInManager
                .Setup(x => x.CheckPasswordSignInAsync(user, "Password123!", true))
                .ReturnsAsync(SignInResult.LockedOut);

            var command = new LoginCommand("test@test.com", "Password123!");

            // Act
            var result = await _handler.Handle(
                command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.InvalidCredentials");

            _tokenService.Verify(
                x => x.GenerateAccessToken(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>()),
                Times.Never);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
