using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Domain.Entities
{
    public class TicketAcceptEscalationTests
    {
        private static (Ticket Ticket, Guid AgentId, EscalationRequest Request) CreateTicketWithEscalationRequest()
        {
            var agentId = Guid.NewGuid();

            var ticket = new Ticket(
                "Ticket title",
                "Ticket description",
                Guid.NewGuid());

            ticket.Claim(agentId, "Original Support Agent");

            var request = new EscalationRequest(
                "Escalation reason",
                ticket.Id,
                agentId,
                Guid.NewGuid());

            ticket.RequestEscalation(
                request,
                "Original Support Agent",
                "Senior Support Agent");

            return (ticket, agentId, request);
        }

        [Fact]
        public void AcceptEscalation_WhenValid_AcceptsRequestAndReassignsTicket()
        {
            // Arrange
            var (ticket, agentId, request) = CreateTicketWithEscalationRequest();

            var administratorId = Guid.NewGuid();
            var newAgentId = Guid.NewGuid();

            var previousActivityCount = ticket.Activities.Count;

            // Act
            ticket.AcceptEscalation(
                request,
                administratorId,
                "Administrator",
                newAgentId,
                "Senior Support Agent",
                "Original Support Agent",
                "Escalation response reason.");

            // Assert
            request.Status.Should().Be(EscalationRequestStatus.Accepted);
            request.RespondedByUserId.Should().Be(administratorId);
            request.NewSupportAgentId.Should().Be(newAgentId);

            ticket.AssignedToId.Should().Be(newAgentId);
            ticket.Activities.Should().HaveCount(previousActivityCount + 1);
        }

        [Fact]
        public void AcceptEscalation_WhenRequestIsNull_ThrowsDomainException()
        {
            // Arrange
            var ticket = new Ticket(
                "Ticket title",
                "Ticket description",
                Guid.NewGuid());

            // Act
            var act = () => ticket.AcceptEscalation(
                null!,
                Guid.NewGuid(),
                "Administrator",
                Guid.NewGuid(),
                "New Agent",
                "Old Agent",
                "Accepted.");

            // Assert
            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void AcceptEscalation_WhenRequestBelongsToAnotherTicket_ThrowsDomainException()
        {
            // Arrange
            var (ticket, _, request) = CreateTicketWithEscalationRequest();

            var otherTicket = new Ticket(
                "Another ticket",
                "Another description",
                Guid.NewGuid());

            // Act
            var act = () => otherTicket.AcceptEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                Guid.NewGuid(),
                "New Agent",
                "Old Agent",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            otherTicket.AssignedToId.Should().BeNull();
        }

        [Fact]
        public void AcceptEscalation_WhenRequestIsNotWaiting_ThrowsDomainException()
        {
            // Arrange
            var (ticket, agentId, request) = CreateTicketWithEscalationRequest();

            request.Reject(
                Guid.NewGuid(),
                "Already rejected.");

            // Act
            var act = () => ticket.AcceptEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                Guid.NewGuid(),
                "New Agent",
                "Old Agent",
                "Accepted.");

            // Assert
            act.Should().Throw<DomainException>();
            ticket.AssignedToId.Should().Be(agentId);
        }

        [Fact]
        public void AcceptEscalation_WhenTicketIsClosed_ThrowsDomainException()
        {
            // Arrange
            var (ticket, agentId, request) = CreateTicketWithEscalationRequest();

            ticket.Resolve();
            ticket.Close();

            // Act
            var act = () => ticket.AcceptEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                Guid.NewGuid(),
                "New Agent",
                "Old Agent",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            ticket.AssignedToId.Should().Be(agentId);
        }

        [Fact]
        public void AcceptEscalation_WhenTicketAssignmentHasChanged_ThrowsDomainException()
        {
            // Arrange
            var (ticket, agentId, request) = CreateTicketWithEscalationRequest();

            ticket.AssignTo(
                Guid.NewGuid(),
                "Administrator",
                Guid.NewGuid(),
                "Another Agent",
                "Original Support Agent");

            var currentAssignee = ticket.AssignedToId;

            // Act
            var act = () => ticket.AcceptEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                Guid.NewGuid(),
                "New Agent",
                "Old Agent",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            ticket.AssignedToId.Should().Be(currentAssignee);
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public void AcceptEscalation_WhenAdministratorIdIsEmpty_ThrowsDomainException()
        {
            // Arrange
            var (ticket, _, request) = CreateTicketWithEscalationRequest();

            // Act
            var act = () => ticket.AcceptEscalation(
                request,
                Guid.Empty,
                "Administrator",
                Guid.NewGuid(),
                "New Agent",
                "Old Agent",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public void AcceptEscalation_WhenNewAgentIdIsEmpty_ThrowsDomainException()
        {
            // Arrange
            var (ticket, _, request) = CreateTicketWithEscalationRequest();

            // Act
            var act = () => ticket.AcceptEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                Guid.Empty,
                "New Agent",
                "Old Agent",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public void AcceptEscalation_WhenAdministratorDisplayNameIsEmpty_ThrowsDomainException()
        {
            // Arrange
            var (ticket, _, request) = CreateTicketWithEscalationRequest();

            // Act
            var act = () => ticket.AcceptEscalation(
                request,
                Guid.NewGuid(),
                " ",
                Guid.NewGuid(),
                "New Agent",
                "Old Agent",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public void AcceptEscalation_WhenNewAgentDisplayNameIsEmpty_ThrowsDomainException()
        {
            // Arrange
            var (ticket, _, request) = CreateTicketWithEscalationRequest();

            // Act
            var act = () => ticket.AcceptEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                Guid.NewGuid(),
                " ",
                "Old Agent",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
        }
    }
}
