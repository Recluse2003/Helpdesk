using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Auth.Login;

namespace Helpdesk.Tests.Unit.Features.Auth.Login
{
    public class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new LoginCommand(
                "test@test.com",
                "Password123!");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyEmail_ShouldHaveError()
        {
            var command = new LoginCommand("", "Password123!");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("test@")]
        [InlineData("@test.com")]
        public void Validate_WithInvalidEmail_ShouldHaveError(string email)
        {
            var command = new LoginCommand(email, "Password123!");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_WithEmailExceedingMaximumLength_ShouldHaveError()
        {
            var email = new string('a', 250) + "@test.com";
            var command = new LoginCommand(email, "Password123!");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_WithEmptyPassword_ShouldHaveError()
        {
            var command = new LoginCommand("test@test.com", "");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Password);
        }
    }
}
