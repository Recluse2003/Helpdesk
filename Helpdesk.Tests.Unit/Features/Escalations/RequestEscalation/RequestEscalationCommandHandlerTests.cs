using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.RequestEscalation;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Escalations.RequestEscalation
{
    public class RequestEscalationCommandHandlerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly HelpdeskDbContext _context;
        private readonly RequestEscalationCommandHandler _handler;

        public RequestEscalationCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);

            _userManagerMock = IdentityMockFactory.CreateUserManager();

            _handler = new RequestEscalationCommandHandler(
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

        private void SetupUser(ApplicationUser user, bool isSupportAgent)
        {
            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, RoleNames.SupportAgent))
                .ReturnsAsync(isSupportAgent);
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

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var command = new RequestEscalationCommand(
                Guid.NewGuid(),
                "Requires specialist assistance.",
                Guid.NewGuid(),
                null);

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
        public async Task Handle_WhenUserIsNotSupportAgent_ReturnsForbidden()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, false);

            var command = new RequestEscalationCommand(
                user.Id,
                "Escalation required.",
                Guid.NewGuid(),
                null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Ticket.EscalationRequestForbidden");
        }

        [Fact]
        public async Task Handle_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true);

            var command = new RequestEscalationCommand(
                user.Id,
                "Escalation required.",
                Guid.NewGuid(),
                null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Ticket.NotFound");
        }

        [Fact]
        public async Task Handle_WhenRequesterIsNotAssignedAgent_ReturnsForbidden()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true);

            var ticket = await CreateAssignedTicket(Guid.NewGuid());

            var command = new RequestEscalationCommand(
                user.Id,
                "Escalation required.",
                ticket.Id,
                null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Ticket.EscalationRequestForbidden");
        }

        [Fact]
        public async Task Handle_WhenProposedRecipientIsRequester_ReturnsValidationError()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true);

            var ticket = await CreateAssignedTicket(user.Id);

            var command = new RequestEscalationCommand(
                user.Id,
                "Escalation required.",
                ticket.Id,
                user.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.SelfAssignmentNotAllowed");
        }

        [Fact]
        public async Task Handle_WhenProposedRecipientDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true);

            var ticket = await CreateAssignedTicket(user.Id);

            var command = new RequestEscalationCommand(
                user.Id,
                "Escalation required.",
                ticket.Id,
                Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.ProposedRecipientNotFound");
        }

        [Fact]
        public async Task Handle_WhenProposedRecipientIsNotASupportAgent_ReturnsValidationError()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true);

            var invalidRecipient = await CreateUser();
            SetupUser(invalidRecipient, false);

            var ticket = await CreateAssignedTicket(user.Id);

            var command = new RequestEscalationCommand(
                user.Id,
                "Escalation required.",
                ticket.Id,
                invalidRecipient.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Id.Should().Be("Escalation.InvalidProposedRecipient");
        }

        [Fact]
        public async Task Handle_WhenEscalationRequestIsValid_ReturnsEscalationRequestIdToNewRequest()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true);

            var validRecipient = await CreateUser();
            SetupUser(validRecipient, true);

            var ticket = await CreateAssignedTicket(user.Id);

            var command = new RequestEscalationCommand(
                user.Id,
                "Escalation required.",
                ticket.Id,
                validRecipient.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeEmpty();

            var savedTicket = await _context.Tickets
                .Include(x => x.EscalationRequests)
                .SingleAsync(x => x.Id == ticket.Id);

            savedTicket.EscalationRequests.Should().ContainSingle();

            var savedRequest = savedTicket.EscalationRequests.Single();

            savedRequest.Id.Should().Be(result.Value);
            savedRequest.RequestedByUserId.Should().Be(user.Id);
            savedRequest.ProposedRecipientUserId.Should().Be(validRecipient.Id);
            savedRequest.Status.Should().Be(EscalationRequestStatus.Waiting);
        }
    }
}
