using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Tickets.GetTicketsByUserId;

namespace Helpdesk.Tests.Unit.Features.Tickets.GetTicketsByUserId
{
    public class GetTicketsByUserIdQueryValidatorTests
    {
        private readonly GetTicketsByUserIdQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ShouldNotHaveErrors()
        {
            var query = new GetTicketsByUserIdQuery(
                Guid.NewGuid(),
                1,
                10);

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithNegativePageNumber_ShouldHaveErrors()
        {
            var query = new GetTicketsByUserIdQuery(
                Guid.NewGuid(),
                -1,
                10);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.PageNumber);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Validate_WithInvalidPageSize_ShouldHaveErrors(int pageSize)
        {
            var query = new GetTicketsByUserIdQuery(
                Guid.NewGuid(),
                1,
                pageSize);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.PageSize);
        }
    }
}
