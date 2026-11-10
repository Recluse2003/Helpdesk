using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Auth.ResendEmailConfirmation;

namespace Helpdesk.Tests.Unit.Features.Auth.ResendConfirmationEmail
{
    public class ResendConfirmationEmailCommandValidatorTests
    {
        private readonly ResendConfirmationEmailCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new ResendConfirmationEmailCommand(
                "test@test.com");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyEmail_ShouldHaveError()
        {
            var command = new ResendConfirmationEmailCommand(
                "");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("test@")]
        [InlineData("@test.com")]
        public void Validate_WithInvalidEmail_ShouldHaveError(string email)
        {
            var command = new ResendConfirmationEmailCommand(
                email);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }
    }
}
