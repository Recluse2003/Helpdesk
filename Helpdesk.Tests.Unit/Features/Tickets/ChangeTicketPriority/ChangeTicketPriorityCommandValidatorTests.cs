using FluentValidation.TestHelper;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalations;
using Helpdesk.Api.Features.Tickets.ChangeTicketPriority;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Tickets.ChangeTicketPriority
{
    public class ChangeTicketPriorityCommandValidatorTests
    {
        private readonly ChangeTicketPriorityCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new ChangeTicketPriorityCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                TicketPriority.Medium);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveErrors()
        {
            var command = new ChangeTicketPriorityCommand(
                Guid.Empty,
                Guid.NewGuid(),
                TicketPriority.Medium);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyTicketId_ShouldHaveErrors()
        {
            var command = new ChangeTicketPriorityCommand(
                Guid.NewGuid(),
                Guid.Empty,
                TicketPriority.Medium);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TicketId);
        }

        [Fact]
        public void Validate_WithInvalidNewPriority_ShouldHaveErrors()
        {
            var command = new ChangeTicketPriorityCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                (TicketPriority)999);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.NewPriority);
        }
    }
}
