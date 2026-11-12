using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.AcceptEscalation;
using Helpdesk.Api.Features.Escalations.GetEscalationDetails;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Escalations.AcceptEscalation
{
    public class AcceptEscalationCommandHandlerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly HelpdeskDbContext _context;
        private readonly AcceptEscalationCommandHandler _handler;

        public AcceptEscalationCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);

            _userManagerMock = IdentityMockFactory.CreateUserManager();

            _handler = new AcceptEscalationCommandHandler(
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

        private async Task<Ticket> CreateAssignedTicket(Guid agentId)
        {
            var ticket = new Ticket(
                "Ticket title",
                "Ticket description.",
                Guid.NewGuid());

            ticket.Claim(agentId, "Support Agent");

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
            var command = new AcceptEscalationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Reason");

            _userManagerMock
                .Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("User.NotFound");
        }

        [Fact]
        public async Task Handle_WhenUserIsNotAdministrator_ReturnsForbidden()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, false, false);

            var command = new AcceptEscalationCommand(user.Id, Guid.NewGuid(), Guid.NewGuid(), "Reason");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.AcceptRequestForbidden");
        }

        [Fact]
        public async Task Handle_WhenEscalationRequestDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var administrator = await CreateUser();
            SetupUser(administrator, false, true);

            var command = new AcceptEscalationCommand(administrator.Id, Guid.NewGuid(), Guid.NewGuid(), "Reason");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.NotFound");
        }


        [Fact]
        public async Task Handle_WhenEscalationRequestHasAlreadyBeenRespondedTo_ReturnsConflict()
        {
            // Arrange
            var escalationRequester = await CreateUser("Requesting Agent");
            SetupUser(escalationRequester, true, false);

            var administrator = await CreateUser("Administrator");
            SetupUser(administrator, false, true);

            var newSupportAgent = await CreateUser("New Support Agent");
            SetupUser(newSupportAgent, true, false);

            var escalationRequest = await CreateActiveEscalationRequest(Guid.NewGuid(), escalationRequester.Id);

            escalationRequest.Accept(
                administrator.Id,
                newSupportAgent.Id,
                "The escalation has been approved.");
            
            var command = new AcceptEscalationCommand(administrator.Id, escalationRequest.Id, Guid.NewGuid(), "Reason");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.AlreadyResponded");
        }

        [Fact]
        public async Task Handle_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var escalationRequester = await CreateUser("Requesting Agent");
            SetupUser(escalationRequester, true, false);

            var administrator = await CreateUser("Administrator");
            SetupUser(administrator, false, true);

            var newSupportAgent = await CreateUser("New Support Agent");
            SetupUser(newSupportAgent, true, false);

            var escalationRequest = await CreateActiveEscalationRequest(Guid.NewGuid(), escalationRequester.Id);

            var command = new AcceptEscalationCommand(administrator.Id, escalationRequest.Id, Guid.NewGuid(), "Reason");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Ticket.NotFound");
        }

        [Fact]
        public async Task Handle_WhenSelectedSupporAentIsTheCurrentOne_ReturnsConflict()
        {
            // Arrange
            var escalationRequester = await CreateUser("Requesting Agent");
            SetupUser(escalationRequester, true, false);

            var administrator = await CreateUser("Administrator");
            SetupUser(administrator, false, true);

            var ticket = await CreateAssignedTicket(escalationRequester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(ticket.Id, escalationRequester.Id);

            var command = new AcceptEscalationCommand(administrator.Id, escalationRequest.Id, escalationRequester.Id, "Reason");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.SameAssignee");
        }

        [Fact]
        public async Task Handle_WhenSelectedSupporAgentDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var escalationRequester = await CreateUser("Requesting Agent");
            SetupUser(escalationRequester, true, false);

            var administrator = await CreateUser("Administrator");
            SetupUser(administrator, false, true);

            var ticket = await CreateAssignedTicket(escalationRequester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(ticket.Id, escalationRequester.Id);

            var command = new AcceptEscalationCommand(administrator.Id, escalationRequest.Id, Guid.NewGuid(), "Reason");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.SupportAgentNotFound");
        }

        [Fact]
        public async Task Handle_WhenSelectedSupporAgentIsNotASupportAgent_ReturnsConflict()
        {
            // Arrange
            var escalationRequester = await CreateUser("Requesting Agent");
            SetupUser(escalationRequester, true, false);

            var administrator = await CreateUser("Administrator");
            SetupUser(administrator, false, true);

            var newSupportAgent = await CreateUser("New Support Agent");
            SetupUser(newSupportAgent, false, false);

            var ticket = await CreateAssignedTicket(escalationRequester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(ticket.Id, escalationRequester.Id);

            var command = new AcceptEscalationCommand(administrator.Id, escalationRequest.Id, newSupportAgent.Id, "Reason");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.InvalidSupportAgent");
        }

        [Fact]
        public async Task Handle_WhenUserIsAdministratorAndCommandIsValid_ReturnsSuccess()
        {
            // Arrange
            var escalationRequester = await CreateUser("Requesting Agent");
            SetupUser(escalationRequester, true, false);

            var administrator = await CreateUser("Administrator");
            SetupUser(administrator, false, true);

            var newSupportAgent = await CreateUser("New Support Agent");
            SetupUser(newSupportAgent, true, false);

            var ticket = await CreateAssignedTicket(escalationRequester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(ticket.Id, escalationRequester.Id);

            var command = new AcceptEscalationCommand(administrator.Id, escalationRequest.Id, newSupportAgent.Id, "Reason");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var updatedEscalationRequest = await _context.EscalationRequests.FindAsync(escalationRequest.Id);

            updatedEscalationRequest.Should().NotBeNull();

            updatedEscalationRequest.Status.Should().Be(EscalationRequestStatus.Accepted);
            updatedEscalationRequest.NewSupportAgentId.Should().Be(newSupportAgent.Id);
            updatedEscalationRequest.ResponseReason.Should().Be("Reason");
        }
    }
}
