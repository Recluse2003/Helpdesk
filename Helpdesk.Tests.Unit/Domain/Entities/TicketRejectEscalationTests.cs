using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Domain.Entities
{
    public class TicketRejectEscalationTests
    {
        private static (Ticket Ticket, Guid AgentId, EscalationRequest Request)
            CreateTicketWithEscalationRequest()
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
        public void RejectEscalation_WhenValid_RejectsRequestAndKeepsOriginalAssignment()
        {
            // Arrange
            var (ticket, agentId, request) = CreateTicketWithEscalationRequest();

            var administratorId = Guid.NewGuid();
            var previousActivityCount = ticket.Activities.Count;

            // Act
            ticket.RejectEscalation(
                request,
                administratorId,
                "Administrator",
                "Escalation response reason.");

            // Assert
            request.Status.Should().Be(EscalationRequestStatus.Rejected);
            request.RespondedByUserId.Should().Be(administratorId);

            ticket.AssignedToId.Should().Be(agentId);
            ticket.Activities.Should().HaveCount(previousActivityCount + 1);
        }

        [Fact]
        public void RejectEscalation_WhenRequestIsNull_ThrowsDomainException()
        {
            // Arrange
            var ticket = new Ticket(
                "Ticket title",
                "Ticket description",
                Guid.NewGuid());

            // Act
            var act = () => ticket.RejectEscalation(
                null!,
                Guid.NewGuid(),
                "Administrator",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void RejectEscalation_WhenRequestBelongsToAnotherTicket_ThrowsDomainException()
        {
            // Arrange
            var (ticket, _, request) = CreateTicketWithEscalationRequest();

            var otherTicket = new Ticket(
                "Another ticket",
                "Another description",
                Guid.NewGuid());

            // Act
            var act = () => otherTicket.RejectEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void RejectEscalation_WhenRequestIsNotWaiting_ThrowsDomainException()
        {
            // Arrange
            var (ticket, agentId, request) = CreateTicketWithEscalationRequest();

            request.Accept(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Already accepted.");

            // Act
            var act = () => ticket.RejectEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                "Rejected.");

            // Assert
            act.Should().Throw<DomainException>();
            ticket.AssignedToId.Should().Be(agentId);
        }

        [Fact]
        public void RejectEscalation_WhenTicketIsClosed_ThrowsDomainException()
        {
            // Arrange
            var (ticket, agentId, request) = CreateTicketWithEscalationRequest();

            ticket.Resolve();
            ticket.Close();

            // Act
            var act = () => ticket.RejectEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            ticket.AssignedToId.Should().Be(agentId);
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public void RejectEscalation_WhenTicketAssignmentHasChanged_ThrowsDomainException()
        {
            // Arrange
            var (ticket, _, request) = CreateTicketWithEscalationRequest();

            ticket.AssignTo(
                Guid.NewGuid(),
                "Administrator",
                Guid.NewGuid(),
                "Another Agent",
                "Original Support Agent");

            var currentAssignee = ticket.AssignedToId;

            // Act
            var act = () => ticket.RejectEscalation(
                request,
                Guid.NewGuid(),
                "Administrator",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            ticket.AssignedToId.Should().Be(currentAssignee);
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public void RejectEscalation_WhenAdministratorIdIsEmpty_ThrowsDomainException()
        {
            // Arrange
            var (ticket, _, request) = CreateTicketWithEscalationRequest();

            // Act
            var act = () => ticket.RejectEscalation(
                request,
                Guid.Empty,
                "Administrator",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public void RejectEscalation_WhenAdministratorDisplayNameIsEmpty_ThrowsDomainException()
        {
            // Arrange
            var (ticket, _, request) = CreateTicketWithEscalationRequest();

            // Act
            var act = () => ticket.RejectEscalation(
                request,
                Guid.NewGuid(),
                " ",
                "Escalation response reason.");

            // Assert
            act.Should().Throw<DomainException>();
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
        }
    }
}
