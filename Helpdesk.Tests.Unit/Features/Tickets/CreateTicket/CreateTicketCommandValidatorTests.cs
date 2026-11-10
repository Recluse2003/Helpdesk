using FluentValidation.TestHelper;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalations;
using Helpdesk.Api.Features.Tickets.CreateTicket;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Tickets.CreateTicket
{
    public class CreateTicketCommandValidatorTests
    {
        private readonly CreateTicketCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new CreateTicketCommand(
                Guid.NewGuid(),
                "Title",
                "Description",
                TicketPriority.Medium);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveErrors()
        {
            var command = new CreateTicketCommand(
                Guid.Empty,
                "Title",
                "Description",
                TicketPriority.Medium);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithLongTitle_ShouldHaveErrors()
        {
            var command = new CreateTicketCommand(
                Guid.NewGuid(),
                new string('a', 250),
                "Description",
                TicketPriority.Medium);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_WithEmptyTitle_ShouldHaveErrors()
        {
            var command = new CreateTicketCommand(
                Guid.NewGuid(),
                string.Empty,
                "Description",
                TicketPriority.Medium);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_WithLongDescription_ShouldHaveErrors()
        {
            var command = new CreateTicketCommand(
                Guid.NewGuid(),
                "Title",
                new string('a', 6000),
                TicketPriority.Medium);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_WithEmptyDescription_ShouldHaveErrors()
        {
            var command = new CreateTicketCommand(
                Guid.NewGuid(),
                "Title",
                string.Empty,
                TicketPriority.Medium);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_WithInvalidPriority_ShouldHaveErrors()
        {
            var command = new CreateTicketCommand(
                Guid.NewGuid(),
                "Title",
                "Description",
                (TicketPriority)999);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Priority);
        }
    }
}
