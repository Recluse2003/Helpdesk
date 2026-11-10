using FluentValidation.TestHelper;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalations;
using Helpdesk.Api.Features.Tickets.ClaimTicket;

namespace Helpdesk.Tests.Unit.Features.Tickets.ClaimTicket
{
    public class ClaimTicketCommandHandlerValidatorTests
    {
        private readonly ClaimTicketCommandHandlerValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new ClaimTicketCommand(
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveErrors()
        {
            var command = new ClaimTicketCommand(
                Guid.Empty,
                Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyTicketId_ShouldHaveErrors()
        {
            var command = new ClaimTicketCommand(
                Guid.NewGuid(),
                Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TicketId);
        }
    }
}
