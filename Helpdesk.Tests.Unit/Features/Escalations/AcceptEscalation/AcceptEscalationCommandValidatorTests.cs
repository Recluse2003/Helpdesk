using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Escalations.AcceptEscalation;
using Helpdesk.Api.Features.Escalations.RejectEscalation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Escalations.AcceptEscalation
{
    public class AcceptEscalationCommandValidatorTests
    {
        private readonly AcceptEscalationCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new AcceptEscalationCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ResponseReason");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveError()
        {
            var command = new AcceptEscalationCommand(
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ResponseReason");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyEscalationRequestId_ShouldHaveError()
        {
            var command = new AcceptEscalationCommand(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                "ResponseReason");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.EscalationRequestId);
        }

        [Fact]
        public void Validate_WithEmptyNewSupportAgentId_ShouldHaveError()
        {
            var command = new AcceptEscalationCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty,
                "ResponseReason");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.NewSupportAgentId);
        }

        [Fact]
        public void Validate_WithLongResponseReason_ShouldHaveError()
        {
            var command = new AcceptEscalationCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new string('a', 6000));

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.ResponseReason);
        }
    }
}
