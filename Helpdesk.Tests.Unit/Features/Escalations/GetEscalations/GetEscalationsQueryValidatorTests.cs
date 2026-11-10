
using FluentValidation.TestHelper;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.AcceptEscalation;
using Helpdesk.Api.Features.Escalations.GetEscalations;

namespace Helpdesk.Tests.Unit.Features.Escalations.GetEscalations
{
    public class GetEscalationsQueryValidatorTests
    {
        private readonly GetEscalationsQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ShouldNotHaveErrors()
        {
            var query = new GetEscalationsQuery(
                1, 
                20, 
                EscalationRequestStatus.Accepted);

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithNegativePageNumber_ShouldHaveErrors()
        {
            var query = new GetEscalationsQuery(
                -1,
                20,
                EscalationRequestStatus.Accepted);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.PageNumber);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Validate_WithInvalidPageSize_ShouldHaveErrors(int pageSize)
        {
            var query = new GetEscalationsQuery(
                1,
                pageSize,
                EscalationRequestStatus.Accepted);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.PageSize);
        }

        [Theory]
        [InlineData((EscalationRequestStatus)999)]
        public void Validate_WithInvalidStatus_ShouldHaveErrors(EscalationRequestStatus? status)
        {
            var query = new GetEscalationsQuery(
                -1,
                20,
                status);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.Status);
        }
    }
}
