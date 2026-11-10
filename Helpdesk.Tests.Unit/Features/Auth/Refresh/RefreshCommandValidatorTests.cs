using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Auth.Refresh;

namespace Helpdesk.Tests.Unit.Features.Auth.Refresh
{
    public class RefreshCommandValidatorTests
    {
        private readonly RefreshCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new RefreshCommand(
                "refresh-token");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyRefreshToken_ShouldHaveError()
        {
            var command = new RefreshCommand(
                string.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
        }

        [Fact]
        public void Validate_WithLongRefreshToken_ShouldHaveError()
        {
            var command = new RefreshCommand(
                new string('a', 120));

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
        }
    }
}
