using FluentValidation.TestHelper;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalations;
using Helpdesk.Api.Features.Tickets.AssignTicket;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Tickets.AssignTicket
{
    public class AssignTicketCommandValidatorTests
    {
        private readonly AssignTicketCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        { 
            var command = new AssignTicketCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveErrors()
        {
            var command = new AssignTicketCommand(
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithEmptyTicketId_ShouldHaveErrors()
        {
            var command = new AssignTicketCommand(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TicketId);
        }

        [Fact]
        public void Validate_WithEmptyAssigneeId_ShouldHaveErrors()
        {
            var command = new AssignTicketCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.AssigneeId);
        }
    }
}
