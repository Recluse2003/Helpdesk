using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Exceptions;

namespace Helpdesk.Tests.Unit.Domain.Entities
{
    public class EscalationRequestTests
    {
        private static EscalationRequest CreateValidRequest(
            string reason = "Response Reason",
            Guid? proposedRecipientUserId = null)
        {
            return new EscalationRequest(
                reason,
                Guid.NewGuid(),
                Guid.NewGuid(),
                proposedRecipientUserId);
        }

        [Fact]
        public void Constructor_WithValidDetails_ShouldCreateEscalationRequest()
        {
            // Arrange
            var reason = "Response Reason";
            var ticketId = Guid.NewGuid();
            var requestedByUserId = Guid.NewGuid();
            var proposedRecipientUserId = Guid.NewGuid();

            // Act
            var request = new EscalationRequest(
                reason,
                ticketId,
                requestedByUserId,
                proposedRecipientUserId);

            // Assert
            request.Id.Should().NotBeEmpty();
            request.Reason.Should().Be(reason);
            request.TicketId.Should().Be(ticketId);
            request.RequestedByUserId.Should().Be(requestedByUserId);
            request.ProposedRecipientUserId.Should().Be(proposedRecipientUserId);
            request.Status.Should().Be(EscalationRequestStatus.Waiting);
            request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            request.ResponseReason.Should().BeNull();
            request.RespondedByUserId.Should().BeNull();
            request.NewSupportAgentId.Should().BeNull();
            request.RespondedAt.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Constructor_WithMissingReason_ShouldThrowDomainException(string reason)
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var requestedByUserId = Guid.NewGuid();

            // Act
            Action act = () => new EscalationRequest(reason, ticketId, requestedByUserId, null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("Escalation reason is required.");
        }

        [Fact]
        public void Constructor_WithReasonExceeding5000Characters_ShouldThrowDomainException()
        {
            // Arrange
            var reason = new string('a', 5001);

            // Act
            Action act = () => new EscalationRequest(reason, Guid.NewGuid(), Guid.NewGuid(), null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("Escalation reason cannot exceed 5000 characters.");
        }

        [Fact]
        public void Constructor_WithEmptyTicketId_ShouldThrowDomainException()
        {
            // Act
            Action act = () => new EscalationRequest("Valid reason", Guid.Empty, Guid.NewGuid(), null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A ticket must be specified.");
        }

        [Fact]
        public void Constructor_WithEmptyRequestingUserId_ShouldThrowDomainException()
        {
            // Act
            Action act = () => new EscalationRequest("Valid reason", Guid.NewGuid(), Guid.Empty, null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A requesting user must be specified.");
        }

        [Fact]
        public void Constructor_WithRequestingUserAsProposedRecipient_ShouldThrowDomainException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            Action act = () => new EscalationRequest("Valid reason", Guid.NewGuid(), userId, userId);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("A requesting user can not propose to escalate a ticket to themself.");
        }

        [Fact]
        public void Accept_WithValidDetails_ShouldAcceptRequest()
        {
            // Arrange
            var request = CreateValidRequest();
            var respondedByUserId = Guid.NewGuid();
            var newSupportAgentId = Guid.NewGuid();
            var responseReason = "Response Reason";

            // Act
            request.Accept(
                respondedByUserId,
                newSupportAgentId,
                responseReason);

            // Assert
            request.Status.Should().Be(EscalationRequestStatus.Accepted);
            request.RespondedByUserId.Should().Be(respondedByUserId);
            request.NewSupportAgentId.Should().Be(newSupportAgentId);
            request.ResponseReason.Should().Be(responseReason);
            request.RespondedAt.Should().NotBeNull();
            request.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Accept_WithNullResponseReason_ShouldAcceptRequest()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            request.Accept(Guid.NewGuid(), Guid.NewGuid(), null);

            // Assert
            request.Status.Should().Be(EscalationRequestStatus.Accepted);
            request.ResponseReason.Should().BeNull();
        }

        [Fact]
        public void Accept_WithEmptyRespondingUserId_ShouldThrowDomainException()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            Action act = () => request.Accept(Guid.Empty, Guid.NewGuid(), null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("The responding user must be specified.");
        }

        [Fact]
        public void Accept_WithEmptyNewSupportAgentId_ShouldThrowDomainException()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            Action act = () => request.Accept(Guid.NewGuid(), Guid.Empty, null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("The new support agent must be specified.");
        }

        [Fact]
        public void Accept_WithResponseReasonExceeding5000Characters_ShouldThrowDomainException()
        {
            // Arrange
            var request = CreateValidRequest();
            var responseReason = new string('a', 5001);

            // Act
            Action act = () => request.Accept(Guid.NewGuid(), Guid.NewGuid(), responseReason);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("Escalation response reason cannot exceed 5000 characters.");
        }

        [Fact]
        public void Accept_WhenRequestAlreadyAccepted_ShouldThrowDomainException()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Accept(Guid.NewGuid(), Guid.NewGuid(), null);

            // Act
            Action act = () => request.Accept(Guid.NewGuid(), Guid.NewGuid(), null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("This escalation request has already been responded to.");
        }

        [Fact]
        public void Accept_WhenRequestAlreadyRejected_ShouldThrowDomainException()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Reject(Guid.NewGuid(), null);

            // Act
            Action act = () => request.Accept(Guid.NewGuid(), Guid.NewGuid(), null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("This escalation request has already been responded to.");
        }

        [Fact]
        public void Reject_WithValidDetails_ShouldRejectRequest()
        {
            // Arrange
            var request = CreateValidRequest();
            var respondedByUserId = Guid.NewGuid();
            var responseReason = "Response Reason.";

            // Act
            request.Reject(respondedByUserId, responseReason);

            // Assert
            request.Status.Should().Be(EscalationRequestStatus.Rejected);
            request.RespondedByUserId.Should().Be(respondedByUserId);
            request.ResponseReason.Should().Be(responseReason);
            request.RespondedAt.Should().NotBeNull();
            request.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            request.NewSupportAgentId.Should().BeNull();
        }

        [Fact]
        public void Reject_WithNullResponseReason_ShouldRejectRequest()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            request.Reject(Guid.NewGuid(), null);

            // Assert
            request.Status.Should().Be(EscalationRequestStatus.Rejected);
            request.ResponseReason.Should().BeNull();
        }

        [Fact]
        public void Reject_WithEmptyRespondingUserId_ShouldThrowDomainException()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            Action act = () => request.Reject(Guid.Empty, null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("The responding user must be specified.");
        }

        [Fact]
        public void Reject_WithResponseReasonExceeding5000Characters_ShouldThrowDomainException()
        {
            // Arrange
            var request = CreateValidRequest();
            var responseReason = new string('a', 5001);

            // Act
            Action act = () => request.Reject(Guid.NewGuid(), responseReason);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("Escalation response reason cannot exceed 5000 characters.");
        }

        [Fact]
        public void Reject_WhenRequestAlreadyRejected_ShouldThrowDomainException()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Reject(Guid.NewGuid(), null);

            // Act
            Action act = () => request.Reject(Guid.NewGuid(), null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("This escalation request has already been responded to.");
        }

        [Fact]
        public void Reject_WhenRequestAlreadyAccepted_ShouldThrowDomainException()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Accept(Guid.NewGuid(), Guid.NewGuid(), null);

            // Act
            Action act = () => request.Reject(Guid.NewGuid(), null);

            // Assert
            act.Should().Throw<DomainException>().WithMessage("This escalation request has already been responded to.");
        }
    }
}
