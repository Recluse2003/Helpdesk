using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Domain.Entities
{
    public class TicketAddPublicMessageTests
    {
        private static Ticket CreateTicket(Guid? creatorId = null)
        {
            return new Ticket(
                "Ticket title",
                "Ticket description",
                creatorId ?? Guid.NewGuid());
        }

        [Fact]
        public void AddPublicMessage_WhenCalledByTicketCreator_AddsMessage()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var ticket = CreateTicket(creatorId);
            var previousUpdatedAt = ticket.UpdatedAt;

            // Act
            ticket.AddPublicMessage("A message.", creatorId);

            // Assert
            ticket.Messages.Should().HaveCount(1);
            ticket.UpdatedAt.Should().BeOnOrAfter(previousUpdatedAt);
        }

        [Fact]
        public void AddPublicMessage_WhenCalledByAssignedAgent_AddsMessage()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var ticket = CreateTicket(creatorId);

            ticket.Claim(agentId, "Support Agent");

            // Act
            ticket.AddPublicMessage("A message.", agentId);

            // Assert
            ticket.Messages.Should().HaveCount(1);
        }

        [Fact]
        public void AddPublicMessage_WhenCalledByUnrelatedUser_ThrowsDomainException()
        {
            // Arrange
            var ticket = CreateTicket();
            var unrelatedUserId = Guid.NewGuid();

            // Act
            var act = () => ticket.AddPublicMessage("A message.", unrelatedUserId);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.Messages.Should().BeEmpty();
        }

        [Fact]
        public void AddPublicMessage_WhenAuthorIdIsEmpty_ThrowsDomainException()
        {
            // Arrange
            var ticket = CreateTicket();

            // Act
            var act = () => ticket.AddPublicMessage("A message.", Guid.Empty);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.Messages.Should().BeEmpty();
        }
    }
}
