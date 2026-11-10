using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Features.Auth.Register;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Auth.Register
{
    public class RegisterCommandHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<IEmailService> _emailService;
        private readonly IConfiguration _configuration;
        private readonly RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();

            _userManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            _emailService = new Mock<IEmailService>();

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://example.com"
                })
                .Build();

            _handler = new RegisterCommandHandler(
                _userManager.Object,
                _configuration,
                _emailService.Object);
        }

        [Fact]
        public async Task Handle_WithExistingEmail_ReturnsConflict()
        {
            // Arrange 
            var command = new RegisterCommand(
                "test test",
                "test@test.com",
                "Password123!");

            _userManager.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(new ApplicationUser());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            _userManager.Verify(
                x => x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>()),
                Times.Never);

            _emailService.Verify(
                x => x.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithValidCommand_CreatesUserAndSendsConfirmationEmail()
        {
            // Arrange
            var command = new RegisterCommand(
                " test test ",
                " test@test.com ",
                "Password123!");

            ApplicationUser? createdUser = null;

            _userManager
                .Setup(x => x.FindByEmailAsync("test@test.com"))
                .ReturnsAsync((ApplicationUser?)null);

            _userManager
                .Setup(x => x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    command.Password))
                .Callback<ApplicationUser, string>(
                    (user, _) => createdUser = user)
                .ReturnsAsync(IdentityResult.Success);

            _userManager
                .Setup(x => x.AddToRoleAsync(
                    It.IsAny<ApplicationUser>(),
                    RoleNames.Customer))
                .ReturnsAsync(IdentityResult.Success);

            _userManager
                .Setup(x => x.GetUserIdAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("user-id");

            _userManager
                .Setup(x => x.GenerateEmailConfirmationTokenAsync(
                    It.IsAny<ApplicationUser>()))
                .ReturnsAsync("confirmation-token");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            createdUser.Should().NotBeNull();
            createdUser!.DisplayName.Should().Be("test test");
            createdUser.Email.Should().Be("test@test.com");
            createdUser.UserName.Should().Be("test@test.com");
            createdUser.EmailConfirmed.Should().BeFalse();

            _userManager.Verify(
                x => x.AddToRoleAsync(createdUser, RoleNames.Customer),
                Times.Once);

            _emailService.Verify(
                x => x.SendEmailAsync(
                    "test@test.com",
                    "Confirm your email address",
                    It.Is<string>(html =>
                        html.Contains("https://example.com/auth/confirm-email") &&
                        html.Contains("userId=user-id") &&
                        html.Contains("code=")),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenUserCreationFails_ReturnsValidationError()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "test@test.com",
                "password");

            var identityError = new IdentityError
            {
                Description = "Password does not meet the requirements."
            };

            _userManager
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _userManager
                .Setup(x => x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    command.Password))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            _userManager.Verify(
                x => x.AddToRoleAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>()),
                Times.Never);

            _emailService.Verify(
                x => x.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenRoleAssignmentFails_DeletesCreatedUser()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "test@test.com",
                "Password123");

            ApplicationUser? createdUser = null;

            _userManager
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _userManager
                .Setup(x => x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    command.Password))
                .Callback<ApplicationUser, string>(
                    (user, _) => createdUser = user)
                .ReturnsAsync(IdentityResult.Success);

            _userManager
                .Setup(x => x.AddToRoleAsync(
                    It.IsAny<ApplicationUser>(),
                    RoleNames.Customer))
                .ReturnsAsync(IdentityResult.Failed());

            _userManager
                .Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            createdUser.Should().NotBeNull();

            _userManager.Verify(
                x => x.DeleteAsync(createdUser!),
                Times.Once);

            _emailService.Verify(
                x => x.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
