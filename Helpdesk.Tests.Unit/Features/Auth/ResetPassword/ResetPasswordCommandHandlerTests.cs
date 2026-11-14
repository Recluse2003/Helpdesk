using FluentAssertions;
using Helpdesk.Api.Features.Auth.ResetPassword;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Auth.ResetPassword
{
    public class ResetPasswordCommandHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly ResetPasswordCommandHandler _handler;

        public ResetPasswordCommandHandlerTests()
        {
            _userManager = IdentityMockFactory.CreateUserManager();
            _handler = new ResetPasswordCommandHandler(_userManager.Object);
        }

        private static ApplicationUser CreateUser(bool emailConfirmed = true)
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

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var command = new ResetPasswordCommand(Guid.NewGuid(), "valid-reset-code", "NewPassword123!");

            _userManager
                .Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.UserNotFound");

            _userManager.Verify(
                x => x.ResetPasswordAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenEmailIsNotConfirmed_ReturnsConflict()
        {
            // Arrange
            var user = CreateUser(emailConfirmed: false);

            var command = new ResetPasswordCommand(user.Id, "valid-reset-code", "NewPassword123!");

            _userManager
                .Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.EmailNotConfirmed");

            _userManager.Verify(
                x => x.ResetPasswordAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenResetCodeIsMalformed_ReturnsConflict()
        {
            // Arrange
            var user = CreateUser();
            var command = new ResetPasswordCommand(user.Id, "invalid", "NewPassword123!");

            _userManager
                .Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.PasswordResetFailure");

            _userManager.Verify(
                x => x.ResetPasswordAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenResetCodeIsInvalidOrExpired_ReturnsConflict()
        {
            // Arrange
            var user = CreateUser();

            var command = new ResetPasswordCommand(user.Id, "invalid", "NewPassword123!");

            _userManager
                .Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync(user);

            _userManager
                .Setup(x => x.ResetPasswordAsync(
                    user,
                    It.IsAny<string>(),
                    command.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = "InvalidToken",
                        Description = "Invalid password reset token."
                    }));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Auth.PasswordResetFailure");
        }

        [Fact]
        public async Task Handle_WhenPasswordResetSucceeds_PassesDecodedCodeToIdentity()
        {
            // Arrange
            const string originalCode = "identity-reset-token+/=";

            var encodedCode = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(originalCode));
            
            var user = CreateUser();

            var command = new ResetPasswordCommand(user.Id, encodedCode, "NewPassword123!");

            _userManager
                .Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync(user);

            _userManager
                .Setup(x => x.ResetPasswordAsync(
                    user,
                    originalCode,
                    command.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _userManager.Verify(
                x => x.ResetPasswordAsync(
                    user,
                    originalCode,
                    command.NewPassword),
                Times.Once);
        }
    }
}
