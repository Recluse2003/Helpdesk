using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Tests.Unit.Domain.Entities
{
    public class TicketRequestEscalationTests
    {
        private static Ticket CreateAssignedTicket(Guid? creatorId = null, Guid? agentId = null)
        {
            var ticket = new Ticket(
                "Ticket title",
                "Ticket description",
                creatorId ?? Guid.NewGuid());

            ticket.Claim(agentId ?? Guid.NewGuid(), "Support Agent");

            return ticket;
        }

        private static EscalationRequest CreateEscalationRequest(Guid ticketId, Guid requesterId, Guid? proposedRecipientId = null)
        {
            return new EscalationRequest(
                "Escalation reason.",
                ticketId,
                requesterId,
                proposedRecipientId);
        }

        [Fact]
        public void RequestEscalation_WhenValid_AddsEscalationRequest()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var ticket = CreateAssignedTicket(agentId: agentId);

            var request = CreateEscalationRequest(ticket.Id, agentId);

            // Act
            ticket.RequestEscalation(
                request,
                "Support Agent",
                null);

            // Assert
            ticket.EscalationRequests.Should().ContainSingle().Which.Should().BeSameAs(request);

            ticket.Activities.Should().HaveCount(3);
        }

        [Fact]
        public void RequestEscalation_WhenTicketIsClosed_ThrowsDomainException()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var ticket = CreateAssignedTicket(agentId: agentId);

            ticket.Resolve();
            ticket.Close();

            var request = CreateEscalationRequest(ticket.Id, agentId);

            // Act
            var act = () => ticket.RequestEscalation(
                request,
                "Support Agent",
                null);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.EscalationRequests.Should().BeEmpty();
        }

        [Fact]
        public void RequestEscalation_WhenRequestedByUnassignedUser_ThrowsDomainException()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var unrelatedUserId = Guid.NewGuid();

            var ticket = CreateAssignedTicket(agentId: agentId);
            var request = CreateEscalationRequest(ticket.Id, unrelatedUserId);

            // Act
            var act = () => ticket.RequestEscalation(
                request,
                "Unrelated User",
                null);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.EscalationRequests.Should().BeEmpty();
        }

        [Fact]
        public void RequestEscalation_WhenAnotherRequestIsWaiting_ThrowsDomainException()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var ticket = CreateAssignedTicket(agentId: agentId);

            var firstRequest = CreateEscalationRequest(ticket.Id, agentId);
            var secondRequest = CreateEscalationRequest(ticket.Id, agentId);

            ticket.RequestEscalation(firstRequest, "Support Agent", null);

            // Act
            var act = () => ticket.RequestEscalation(
                secondRequest,
                "Support Agent",
                null);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.EscalationRequests.Should().ContainSingle();
        }

        [Fact]
        public void RequestEscalation_WhenRequesterDisplayNameIsEmpty_ThrowsDomainException()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var ticket = CreateAssignedTicket(agentId: agentId);
            var request = CreateEscalationRequest(ticket.Id, agentId);

            // Act
            var act = () => ticket.RequestEscalation(request, " ", null);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.EscalationRequests.Should().BeEmpty();
        }

        [Fact]
        public void RequestEscalation_WhenProposedRecipientHasNoDisplayName_ThrowsDomainException()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var recipientId = Guid.NewGuid();

            var ticket = CreateAssignedTicket(agentId: agentId);
            var request = CreateEscalationRequest(ticket.Id, agentId, recipientId);

            // Act
            var act = () => ticket.RequestEscalation(
                request,
                "Support Agent",
                null);

            // Assert
            act.Should().Throw<DomainException>();
            ticket.EscalationRequests.Should().BeEmpty();
        }

        [Fact]
        public void RequestEscalation_WhenProposedRecipientHasDisplayName_AddsRequest()
        {
            // Arrange
            var agentId = Guid.NewGuid();
            var recipientId = Guid.NewGuid();

            var ticket = CreateAssignedTicket(agentId: agentId);
            var request = CreateEscalationRequest(ticket.Id, agentId, recipientId);

            // Act
            ticket.RequestEscalation(
                request,
                "Support Agent",
                "Senior Support Agent");

            // Assert
            ticket.EscalationRequests.Should().ContainSingle().Which.Should().BeSameAs(request);
        }
    }
}
