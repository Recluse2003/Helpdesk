using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Auth.Register;

namespace Helpdesk.Tests.Unit.Features.Auth.Register
{
    public class RegisterCommandValidatorTests
    {
        private readonly RegisterCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            // Arrange 
            var command = new RegisterCommand(
                "test test",
                "test@test.com",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithOverMaxLengthDisplayName_ShouldHaveErrors()
        {
            // Arrange 
            var command = new RegisterCommand(
                "testtesttesttesttesttesttesttesttesttesttesttesttesttest",
                "test@test.com",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DisplayName);
        }

        [Fact]
        public void Validate_WithEmptyDisplayName_ShouldHaveError()
        {
            // Arrange 
            var command = new RegisterCommand(
                "",
                "test@test.com",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DisplayName);
        }

        [Fact]
        public void Validate_WithEmptyEmail_ShouldHaveError()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_WithInvalidEmail_ShouldHaveError()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "not-an-email",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_WithVeryLongEmail_ShouldHaveError()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest@test.com",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_WithEmptyPassword_ShouldHaveError()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "test@test.com",
                "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_WithShortPassword_ShouldHaveError()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "test@test.com",
                "pass");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_WithPasswordMissingUppercase_ShouldHaveError()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "test@test.com",
                "password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_WithPasswordMissingLowercase_ShouldHaveError()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "test@test.com",
                "PASSWORD123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_WithPasswordMissingNumber_ShouldHaveError()
        {
            // Arrange
            var command = new RegisterCommand(
                "test test",
                "test@test.com",
                "Password!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }
    }
}
