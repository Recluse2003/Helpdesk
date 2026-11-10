using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Escalations.RejectEscalation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Escalations.RejectEscalation
{
    public class RejectEscalationCommandValidatorTests
    {
        private readonly RejectEscalationCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new RejectEscalationCommand(
                Guid.NewGuid(), 
                Guid.NewGuid(),
                "ResponseReason");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveError()
        {
            var command = new RejectEscalationCommand(
                Guid.Empty,
                Guid.NewGuid(),
                "ResponseReason");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyEscalationRequestId_ShouldHaveError()
        {
            var command = new RejectEscalationCommand(
                Guid.NewGuid(),
                Guid.Empty,
                "ResponseReason");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.EscalationRequestId);
        }

        [Fact]
        public void Validate_WithLongResponseReason_ShouldHaveError()
        {
            var command = new RejectEscalationCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new string('a', 6000));

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.ResponseReason);
        }
    }
}
