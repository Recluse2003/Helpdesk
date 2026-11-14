using FluentAssertions;
using Helpdesk.Api.Features.Auth.ResendEmailConfirmation;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Auth.ResendConfirmationEmail
{
    public class ResendConfirmationEmailCommandHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly IConfiguration _configuration;
        private readonly ResendConfirmationEmailCommandHandler _handler;

        public ResendConfirmationEmailCommandHandlerTests()
        {
            _userManagerMock = IdentityMockFactory.CreateUserManager();
            _emailServiceMock = new Mock<IEmailService>();

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://example.com"
                })
                .Build();

            _handler = new ResendConfirmationEmailCommandHandler(
                _userManagerMock.Object,
                _configuration,
                _emailServiceMock.Object);
        }

        private static ApplicationUser CreateUser(bool emailConfirmed = false)
        {
            return new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                Email = "test@example.com",
                DisplayName = "Test User",
                EmailConfirmed = emailConfirmed
            };
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsTrue()
        {
            // Arrange
            var command = new ResendConfirmationEmailCommand("missing@example.com");

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert - Returns true to prevent another user from discovering which emails have signed up. 
            result.IsSuccess.Should().BeTrue();

            _emailServiceMock.Verify(
                x => x.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenEmailAlreadyConfirmed_ReturnsTrue()
        {
            // Arrange
            var user = CreateUser(emailConfirmed: true);
            var command = new ResendConfirmationEmailCommand(user.Email!);

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert - Returns true to prevent another user from discovering which emails have signed up. 
            result.IsSuccess.Should().BeTrue();

            _userManagerMock.Verify(
                x => x.GenerateEmailConfirmationTokenAsync(
                    It.IsAny<ApplicationUser>()),
                Times.Never);

            _emailServiceMock.Verify(
                x => x.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenUserIsUnconfirmed_SendsConfirmationEmail()
        {
            // Arrange
            var user = CreateUser();
            var command = new ResendConfirmationEmailCommand(user.Email!);
            var confirmationToken = "test-confirmation-token";
            var cancellationToken = CancellationToken.None;

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.GetUserIdAsync(user))
                .ReturnsAsync(user.Id.ToString());

            _userManagerMock
                .Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
                .ReturnsAsync(confirmationToken);

            _emailServiceMock
                .Setup(x => x.SendEmailAsync(
                    user.Email!,
                    "Confirm your email address",
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
                    "Confirm your email address",
                    It.Is<string>(body =>
                        body.Contains("https://example.com/auth/confirm-email") &&
                        body.Contains($"userId={user.Id}") &&
                        body.Contains("code=") &&
                        body.Contains("Confirm your email")),
                    cancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task Handle_EncodesConfirmationTokenUsingBase64Url()
        {
            // Arrange
            var user = CreateUser();
            var command = new ResendConfirmationEmailCommand(user.Email!);
            var confirmationToken = "token+/=with-special-characters";

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.GetUserIdAsync(user))
                .ReturnsAsync(user.Id.ToString());

            _userManagerMock
                .Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
                .ReturnsAsync(confirmationToken);

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

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));

            capturedBody.Should().Contain($"code={encodedToken}");
        }
    }
}
