using FluentValidation.TestHelper;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalations;
using Helpdesk.Api.Features.Tickets.AddTicketMessage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Tickets.AddTicketMessage
{
    public class AddTicketMessageCommandValidatorTests
    {
        private readonly AddTicketMessageCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new AddTicketMessageCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Content",
                true);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveErrors()
        {
            var command = new AddTicketMessageCommand(
                Guid.Empty,
                Guid.NewGuid(),
                "Content",
                true);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyTicketId_ShouldHaveErrors()
        {
            var command = new AddTicketMessageCommand(
                Guid.NewGuid(),
                Guid.Empty,
                "Content",
                true);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TicketId);
        }

        [Fact]
        public void Validate_WithEmptyContent_ShouldHaveErrors()
        {
            var command = new AddTicketMessageCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                string.Empty,
                true);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Content);
        }

        [Fact]
        public void Validate_WithLongContent_ShouldHaveErrors()
        {
            var command = new AddTicketMessageCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new string('a', 6000),
                true);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Content);
        }
    }
}
