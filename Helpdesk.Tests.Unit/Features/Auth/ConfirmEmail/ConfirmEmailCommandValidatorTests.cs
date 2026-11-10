

using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Auth.ConfirmEmail;
using Helpdesk.Api.Features.Auth.Login;

namespace Helpdesk.Tests.Unit.Features.Auth.ConfirmEmail
{
    public class ConfirmEmailCommandValidatorTests
    {
        private readonly ConfirmEmailCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new ConfirmEmailCommand(
                Guid.NewGuid(),
                "Code");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveError()
        {
            var command = new ConfirmEmailCommand(
                Guid.Empty,
                "Code");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyCodeId_ShouldHaveError()
        {
            var command = new ConfirmEmailCommand(
                Guid.NewGuid(),
                string.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Code);
        }
    }
}
