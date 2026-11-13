
using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.RequestEscalation;
using Helpdesk.Api.Features.Tickets.ClaimTicket;
using Helpdesk.Api.Features.Tickets.CreateTicket;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Tickets.ClaimTicket
{
    public class ClaimTicketCommandHandlerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly HelpdeskDbContext _context;
        private readonly ClaimTicketCommandHandler _handler;

        public ClaimTicketCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);

            _userManagerMock = IdentityMockFactory.CreateUserManager();

            _handler = new ClaimTicketCommandHandler(
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

        private async Task<Ticket> CreateTicket()
        {
            var ticket = new Ticket(
                "Ticket title",
                "Ticket description.",
                Guid.NewGuid());

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return ticket;
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var command = new ClaimTicketCommand(
                Guid.NewGuid(),
                Guid.NewGuid());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("User.NotFound");
        }

        [Fact]
        public async Task Handle_WhenUserIsNotSupportAgent_ReturnsForbidden()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, false);

            var command = new ClaimTicketCommand(
                user.Id,
                Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Ticket.TicketClaimForbidden");
        }

        [Fact]
        public async Task Handle_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true);

            var command = new ClaimTicketCommand(
                user.Id,
                Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Ticket.NotFound");
        }

        [Fact]
        public async Task Handle_WhenTicketIsAlreadyAssigned_ReturnsConflict()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true);

            var ticket = await CreateTicket();

            ticket.Claim(user.Id, user.DisplayName);

            var command = new ClaimTicketCommand(
                user.Id,
                ticket.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Ticket.AlreadyAssigned");
        }

        [Fact]
        public async Task Handle_WhenRequestIsValid_ReturnsSuccessAndClaimTicket()
        {
            // Arrange
            var user = await CreateUser();
            SetupUser(user, true);

            var ticket = await CreateTicket();

            var command = new ClaimTicketCommand(
                user.Id,
                ticket.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var claimedTicket = await _context.Tickets
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == ticket.Id);

            claimedTicket.Should().NotBeNull();
            claimedTicket!.AssignedToId.Should().Be(user.Id);
        }
    }
}
