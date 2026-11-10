using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Tickets.GetTicketDetails;

namespace Helpdesk.Tests.Unit.Features.Tickets.GetTicketDetails
{
    public class GetTicketDetailsQueryValidatorTests
    {
        private readonly GetTicketDetailsQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var query = new GetTicketDetailsQuery(
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveErrors()
        {
            var query = new GetTicketDetailsQuery(
                Guid.Empty,
                Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyTicketId_ShouldHaveErrors()
        {
            var query = new GetTicketDetailsQuery(
                Guid.NewGuid(),
                Guid.Empty);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.TicketId);
        }
    }
}
