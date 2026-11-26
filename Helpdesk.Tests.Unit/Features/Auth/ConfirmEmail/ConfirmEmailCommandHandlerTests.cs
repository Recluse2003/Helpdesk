using FluentAssertions;
using Helpdesk.Api.Features.Auth.ConfirmEmail;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Auth.ConfirmEmail
{
    public class ConfirmEmailCommandHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly ConfirmEmailCommandHandler _handler;

        public ConfirmEmailCommandHandlerTests()
        {
            _userManagerMock = IdentityMockFactory.CreateUserManager();
            _handler = new ConfirmEmailCommandHandler(_userManagerMock.Object);
        }

        private static ApplicationUser CreateUser(bool emailConfirmed = false)
        {
            return new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"user-{Guid.NewGuid()}@example.com",
                Email = $"user-{Guid.NewGuid()}@example.com",
                DisplayName = "Test User",
                EmailConfirmed = emailConfirmed
            };
        }

        private static ConfirmEmailCommand CreateCommand(Guid userId, string code = "valid-code")
        {
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            return new ConfirmEmailCommand(userId, encodedCode);
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateCommand(userId);

            _userManagerMock
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.UserNotFound");

            _userManagerMock.Verify(
                x => x.ConfirmEmailAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenEmailAlreadyConfirmed_ReturnsConflict()
        {
            // Arrange
            var user = CreateUser(emailConfirmed: true);
            var command = CreateCommand(user.Id);

            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.EmailAlreadyConfirmed");

            _userManagerMock.Verify(
                x => x.ConfirmEmailAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenCodeIsMalformed_ReturnsConflict()
        {
            // Arrange
            var user = CreateUser();
            var command = new ConfirmEmailCommand(user.Id, "*invalid*");

            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.EmailConfirmationFailure");

            _userManagerMock.Verify(
                x => x.ConfirmEmailAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenIdentityRejectsCode_ReturnsConflict()
        {
            // Arrange
            var user = CreateUser();
            var confirmationCode = "invalid-or-expired-token";
            var command = CreateCommand(user.Id, confirmationCode);

            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.ConfirmEmailAsync(user, confirmationCode))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError { Description = "Invalid token." }));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.EmailConfirmationFailure");
        }

        [Fact]
        public async Task Handle_WhenConfirmationSucceeds_ReturnsSuccess()
        {
            // Arrange
            var user = CreateUser();
            var confirmationCode = "valid-confirmation-token";
            var command = CreateCommand(user.Id, confirmationCode);

            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.ConfirmEmailAsync(user, confirmationCode))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _userManagerMock.Verify(
                x => x.ConfirmEmailAsync(user, confirmationCode),
                Times.Once);
        }

        [Fact]
        public async Task Handle_DecodesBase64UrlCodeBeforePassingItToIdentity()
        {
            // Arrange
            var user = CreateUser();
            var originalCode = "confirmation-token+/=";
            var encodedCode = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(originalCode));

            var command = new ConfirmEmailCommand(user.Id, encodedCode);

            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.ConfirmEmailAsync(user, originalCode))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _userManagerMock.Verify(
                x => x.ConfirmEmailAsync(user, originalCode),
                Times.Once);
        }
    }
}
