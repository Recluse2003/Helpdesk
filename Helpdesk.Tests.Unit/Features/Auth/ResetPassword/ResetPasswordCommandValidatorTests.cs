using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Auth.Login;
using Helpdesk.Api.Features.Auth.Register;
using Helpdesk.Api.Features.Auth.ResetPassword;

namespace Helpdesk.Tests.Unit.Features.Auth.ResetPassword
{
    public class ResetPasswordCommandValidatorTests
    {
        private readonly ResetPasswordCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new ResetPasswordCommand(
                Guid.NewGuid(),
                "Code",
                "Password123!");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveError()
        {
            // Arrange
            var command = new ResetPasswordCommand(
                Guid.Empty,
                "Code",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyCode_ShouldHaveError()
        {
            // Arrange
            var command = new ResetPasswordCommand(
                Guid.NewGuid(),
                string.Empty,
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void Validate_WithEmptyPassword_ShouldHaveError()
        {
            // Arrange
            var command = new ResetPasswordCommand(
                Guid.NewGuid(),
                "Code",
                string.Empty);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Fact]
        public void Validate_WithShortPassword_ShouldHaveError()
        {
            // Arrange
            var command = new ResetPasswordCommand(
                Guid.NewGuid(),
                "Code",
                "pass");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Fact]
        public void Validate_WithLongPassword_ShouldHaveError()
        {
            // Arrange
            var command = new ResetPasswordCommand(
                Guid.NewGuid(),
                "Code",
                new string('a', 126) + "1B!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Fact]
        public void Validate_WithPasswordMissingUppercase_ShouldHaveError()
        {
            // Arrange
            var command = new ResetPasswordCommand(
                Guid.NewGuid(),
                "Code",
                "password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Fact]
        public void Validate_WithPasswordMissingLowercase_ShouldHaveError()
        {
            // Arrange
            var command = new ResetPasswordCommand(
                Guid.NewGuid(),
                "Code",
                "PASSWORD123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Fact]
        public void Validate_WithPasswordMissingNumber_ShouldHaveError()
        {
            // Arrange
            var command = new ResetPasswordCommand(
                Guid.NewGuid(),
                "Code",
                "Password!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }
    }
}
