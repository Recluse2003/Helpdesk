using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Tests.Unit.Domain.Entities
{
    public class TicketAddInternalNoteTests
    {
        private static Ticket CreateTicket(Guid? creatorId = null)
        {
            return new Ticket(
                "Ticket title",
                "Ticket description",
                creatorId ?? Guid.NewGuid());
        }

        [Fact]
        public void AddInternalNote_WhenCalledByAssignedAgent_AddsNote()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var ticket = CreateTicket(creatorId);

            ticket.Claim(agentId, "Support Agent");

            // Act
            ticket.AddInternalNote("Internal note.", agentId);

            // Assert
            ticket.Messages.Should().HaveCount(1);
        }

        [Fact]
        public void AddInternalNote_WhenCalledByTicketCreator_ThrowsDomainException()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var ticket = CreateTicket(creatorId);

            // Act
            var act = () => ticket.AddInternalNote("Internal note.", creatorId);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.Messages.Should().BeEmpty();
        }

        [Fact]
        public void AddInternalNote_WhenCalledByUnassignedAgent_ThrowsDomainException()
        {
            // Arrange
            var ticket = CreateTicket();
            var agentId = Guid.NewGuid();

            // Act
            var act = () => ticket.AddInternalNote("Internal note.", agentId);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.Messages.Should().BeEmpty();
        }

        [Fact]
        public void AddInternalNote_WhenAuthorIdIsEmpty_ThrowsDomainException()
        {
            // Arrange
            var ticket = CreateTicket();

            // Act
            var act = () => ticket.AddInternalNote("Internal note.", Guid.Empty);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.Messages.Should().BeEmpty();
        }
    }
}
