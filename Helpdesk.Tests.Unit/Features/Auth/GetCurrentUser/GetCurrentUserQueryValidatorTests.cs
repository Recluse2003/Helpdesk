using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Auth.GetCurrentUser;

namespace Helpdesk.Tests.Unit.Features.Auth.GetCurrentUser
{
    public class GetCurrentUserQueryValidatorTests
    {
        private readonly GetCurrentUserQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ShouldNotHaveErrors()
        {
            var query = new GetCurrentUserQuery(
                Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveError()
        {
            var query = new GetCurrentUserQuery(
                Guid.Empty);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }
    }
}
