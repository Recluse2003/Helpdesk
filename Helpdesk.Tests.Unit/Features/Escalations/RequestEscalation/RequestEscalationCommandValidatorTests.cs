using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Escalations.RequestEscalation;

namespace Helpdesk.Tests.Unit.Features.Escalations.RequestEscalation
{
    public class RequestEscalationCommandValidatorTests
    {
        private readonly RequestEscalationCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new RequestEscalationCommand(
                Guid.NewGuid(), 
                "Reason",
                Guid.NewGuid(),
                Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveError()
        {
            var command = new RequestEscalationCommand(
                Guid.Empty,
                "Reason",
                Guid.NewGuid(),
                Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyReason_ShouldHaveError()
        {
            var command = new RequestEscalationCommand(
                Guid.NewGuid(),
                string.Empty,
                Guid.NewGuid(),
                Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public void Validate_WithLongReason_ShouldHaveError()
        {
            var command = new RequestEscalationCommand(
                Guid.NewGuid(),
                new string('a', 6000),
                Guid.NewGuid(),
                Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public void Validate_WithEmptyTicketId_ShouldHaveErrors()
        {
            var command = new RequestEscalationCommand(
                Guid.NewGuid(),
                "Reason",
                Guid.Empty,
                Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TicketId);
        }
    }
}
