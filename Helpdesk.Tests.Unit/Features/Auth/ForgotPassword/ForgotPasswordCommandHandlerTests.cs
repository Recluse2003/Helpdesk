using FluentAssertions;
using Helpdesk.Api.Features.Auth.ForgotPassword;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Auth.ForgotPassword
{
    public class ForgotPasswordCommandHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly IConfiguration _configuration;
        private readonly ForgotPasswordCommandHandler _handler;

        public ForgotPasswordCommandHandlerTests()
        {
            _userManagerMock = IdentityMockFactory.CreateUserManager();
            _emailServiceMock = new Mock<IEmailService>();

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://example.com"
                })
                .Build();

            _handler = new ForgotPasswordCommandHandler(
                _userManagerMock.Object,
                _configuration,
                _emailServiceMock.Object);
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

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsSuccessWithoutSendingEmail()
        {
            // Arrange
            var command = new ForgotPasswordCommand("missing@example.com");

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _emailServiceMock.Verify(
                x => x.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _userManagerMock.Verify(
                x => x.GeneratePasswordResetTokenAsync(
                    It.IsAny<ApplicationUser>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenEmailIsNotConfirmed_ReturnsSuccessWithoutSendingEmail()
        {
            // Arrange
            var user = CreateUser(emailConfirmed: false);
            var command = new ForgotPasswordCommand(user.Email!);

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _emailServiceMock.Verify(
                x => x.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _userManagerMock.Verify(
                x => x.GeneratePasswordResetTokenAsync(
                    It.IsAny<ApplicationUser>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenUserIsConfirmed_GeneratesPasswordResetToken()
        {
            // Arrange
            var user = CreateUser(emailConfirmed: true);
            var command = new ForgotPasswordCommand(user.Email!);
            var token = "password-reset-token";

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync(token);

            _emailServiceMock
                .Setup(x => x.SendEmailAsync(
                    user.Email!,
                    "Reset your password",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _userManagerMock.Verify(
                x => x.GeneratePasswordResetTokenAsync(user),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenUserIsConfirmed_SendsPasswordResetEmail()
        {
            // Arrange
            var user = CreateUser(emailConfirmed: true);
            var command = new ForgotPasswordCommand(user.Email!);
            var token = "password-reset-token";
            var cancellationToken = CancellationToken.None;

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync(token);

            _emailServiceMock
                .Setup(x => x.SendEmailAsync(
                    user.Email!,
                    "Reset your password",
                    It.IsAny<string>(),
                    cancellationToken))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _emailServiceMock.Verify(
                x => x.SendEmailAsync(
                    user.Email!,
                    "Reset your password",
                    It.Is<string>(body =>
                        body.Contains("https://example.com/auth/reset-password") &&
                        body.Contains($"userId={user.Id}") &&
                        body.Contains("code=") &&
                        body.Contains("Reset your password")),
                    cancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task Handle_EncodesPasswordResetTokenUsingBase64Url()
        {
            // Arrange
            var user = CreateUser(emailConfirmed: true);
            var command = new ForgotPasswordCommand(user.Email!);
            var token = "token+/=with-special-characters";

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync(token);

            string? capturedBody = null;

            _emailServiceMock
                .Setup(x => x.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>(
                    (_, _, body, _) => capturedBody = body)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedBody.Should().NotBeNull();

            var encodedToken = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(token));

            capturedBody.Should().Contain($"code={encodedToken}");
        }

        [Fact]
        public async Task Handle_WhenSendingEmailFails_PropagatesException()
        {
            // Arrange
            var user = CreateUser(emailConfirmed: true);
            var command = new ForgotPasswordCommand(user.Email!);

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("password-reset-token");

            _emailServiceMock
                .Setup(x => x.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Email service unavailable."));

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Email service unavailable.");
        }
    }
}
