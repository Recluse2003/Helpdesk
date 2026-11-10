using FluentValidation.TestHelper;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalationDetails;
using Helpdesk.Api.Features.Escalations.GetEscalations;

namespace Helpdesk.Tests.Unit.Features.Escalations.GetEscalationDetails
{
    public class GetEscalationDetailsQueryValidatorTests
    {
        private readonly GetEscalationDetailsQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ShouldNotHaveErrors()
        {
            var command = new GetEscalationDetailsQuery(
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveErrors()
        {
            var command = new GetEscalationDetailsQuery(
                Guid.Empty,
                Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyEscalationRequestId_ShouldHaveErrors()
        {
            var command = new GetEscalationDetailsQuery(
                Guid.NewGuid(),
                Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.EscalationRequestId);
        }
    }
}
