using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalationDetails;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Helpdesk.Tests.Unit.Features.Escalations.GetEscalationDetails
{
    public class GetEscalationDetailsQueryHandlerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly HelpdeskDbContext _context;
        private readonly GetEscalationDetailsQueryHandler _handler;

        public GetEscalationDetailsQueryHandlerTests() 
        {
            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);

            _userManagerMock = IdentityMockFactory.CreateUserManager();

            _handler = new GetEscalationDetailsQueryHandler(
                _context,
                _userManagerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<ApplicationUser> CreateUser(string displayName = "Test User")
        {
            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"user-{Guid.NewGuid()}@example.com",
                Email = $"user-{Guid.NewGuid()}@example.com",
                DisplayName = displayName
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }

        private void SetupUser(ApplicationUser user, bool isSupportAgent, bool isAdmin)
        {
            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, RoleNames.SupportAgent))
                .ReturnsAsync(isSupportAgent);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, RoleNames.Administrator))
                .ReturnsAsync(isAdmin);
        }

        private async Task<Ticket> CreateTicket(Guid creatorId)
        {
            var ticket = new Ticket(
                "Ticket title",
                "Ticket description.",
                creatorId);

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return ticket;
        }

        private async Task<EscalationRequest> CreateActiveEscalationRequest(
            Guid ticketId, 
            Guid requestedByUserId, 
            Guid? proposedRecipientUserId = null)
        {
            var escalationRequest = new EscalationRequest(
                "Escalation reason.",
                ticketId,
                requestedByUserId,
                proposedRecipientUserId);

            _context.EscalationRequests.Add(escalationRequest);
            await _context.SaveChangesAsync();

            return escalationRequest;
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var query = new GetEscalationDetailsQuery(Guid.NewGuid(), Guid.NewGuid());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(query.UserId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("User.NotFound");
        }

        [Fact]
        public async Task Handle_WhenEscalationRequestDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true, false);

            var query = new GetEscalationDetailsQuery(user.Id, Guid.NewGuid());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.NotFound");
        }

        [Fact]
        public async Task Handle_WhenUserIsNotAdminOrEscalationRequester_ReturnsForbidden()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true, false);

            var escalationRequest = await CreateActiveEscalationRequest(Guid.NewGuid(), Guid.NewGuid());

            var query = new GetEscalationDetailsQuery(user.Id, escalationRequest.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.ReviewForbidden");
        }

        [Fact]
        public async Task Handle_WhenEscalationRequestExistsButItsTicketDoesNot_ReturnsNotFound()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true, false);

            var escalationRequest = await CreateActiveEscalationRequest(Guid.NewGuid(), user.Id);

            var query = new GetEscalationDetailsQuery(user.Id, escalationRequest.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Ticket.NotFound");
        }

        [Fact]
        public async Task Handle_WhenUserIsEscalationRequester_ReturnsEscalationDetails()
        {
            // Arrange
            var requester = await CreateUser("Requesting Agent");
            SetupUser(requester, true, false);

            var ticket = await CreateTicket(requester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(
                ticket.Id,
                requester.Id);

            var query = new GetEscalationDetailsQuery(
                requester.Id,
                escalationRequest.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.TicketId.Should().Be(ticket.Id);
            result.Value.TicketTitle.Should().Be(ticket.Title);

            result.Value.RequestedByUserId.Should().Be(requester.Id);
            result.Value.RequestedByDisplayName.Should().Be(requester.DisplayName);

            result.Value.Reason.Should().Be("Escalation reason.");
            result.Value.Status.Should().Be(EscalationRequestStatus.Waiting);

            result.Value.ProposedRecipientUserId.Should().BeNull();
            result.Value.ProposedRecipientDisplayName.Should().BeNull();

            result.Value.RespondedByUserId.Should().BeNull();
            result.Value.NewSupportAgentId.Should().BeNull();
            result.Value.ResponseReason.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WhenUserIsAdministrator_ReturnsEscalationDetails()
        {
            // Arrange
            var requester = await CreateUser("Requesting Agent");
            SetupUser(requester, true, false);

            var administrator = await CreateUser("Administrator");
            SetupUser(administrator, false, true);

            var ticket = await CreateTicket(requester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(
                ticket.Id,
                requester.Id);

            var query = new GetEscalationDetailsQuery(
                administrator.Id,
                escalationRequest.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.TicketId.Should().Be(ticket.Id);
            result.Value.RequestedByUserId.Should().Be(requester.Id);
            result.Value.RequestedByDisplayName.Should().Be(requester.DisplayName);
        }

        [Fact]
        public async Task Handle_WhenEscalationRequestHasBeenAccepted_ReturnsAllRelevantUserDetails()
        {
            // Arrange
            var requester = await CreateUser("Requesting Agent");
            SetupUser(requester, true, false);

            var proposedRecipient = await CreateUser("Proposed Agent");
            SetupUser(proposedRecipient, true, false);

            var administrator = await CreateUser("Administrator");
            SetupUser(administrator, false, true);

            var newSupportAgent = await CreateUser("New Support Agent");
            SetupUser(newSupportAgent, true, false);

            var ticket = await CreateTicket(requester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(
                ticket.Id,
                requester.Id,
                proposedRecipient.Id);

            escalationRequest.Accept(
                administrator.Id,
                newSupportAgent.Id,
                "The escalation has been approved.");

            await _context.SaveChangesAsync();

            var query = new GetEscalationDetailsQuery(
                administrator.Id,
                escalationRequest.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.RequestedByUserId.Should().Be(requester.Id);
            result.Value.RequestedByDisplayName.Should().Be(requester.DisplayName);

            result.Value.ProposedRecipientUserId.Should().Be(proposedRecipient.Id);
            result.Value.ProposedRecipientDisplayName.Should().Be(proposedRecipient.DisplayName);

            result.Value.RespondedByUserId.Should().Be(administrator.Id);
            result.Value.RespondedByDisplayName.Should().Be(administrator.DisplayName);

            result.Value.NewSupportAgentId.Should().Be(newSupportAgent.Id);
            result.Value.NewSupportAgentDisplayName.Should().Be(newSupportAgent.DisplayName);

            result.Value.ResponseReason.Should().Be("The escalation has been approved.");
            result.Value.Status.Should().Be(EscalationRequestStatus.Accepted);
        }

        [Fact]
        public async Task Handle_WhenEscalationRequestHasBeenRejected_ReturnsResponseDetailsWithoutNewSupportAgent()
        {
            // Arrange
            var requester = await CreateUser("Requesting Agent");
            SetupUser(requester, true, false);

            var administrator = await CreateUser("Administrator");
            SetupUser(administrator, false, true);

            var ticket = await CreateTicket(requester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(
                ticket.Id,
                requester.Id);

            escalationRequest.Reject(
                administrator.Id,
                "The escalation request has been rejected.");

            await _context.SaveChangesAsync();

            var query = new GetEscalationDetailsQuery(
                administrator.Id,
                escalationRequest.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.RequestedByUserId.Should().Be(requester.Id);
            result.Value.RequestedByDisplayName.Should().Be(requester.DisplayName);

            result.Value.RespondedByUserId.Should().Be(administrator.Id);
            result.Value.RespondedByDisplayName.Should().Be(administrator.DisplayName);

            result.Value.Status.Should().Be(EscalationRequestStatus.Rejected);
            result.Value.ResponseReason.Should().Be("The escalation request has been rejected.");

            result.Value.NewSupportAgentId.Should().BeNull();
            result.Value.NewSupportAgentDisplayName.Should().BeNull();
        }
    }
}
