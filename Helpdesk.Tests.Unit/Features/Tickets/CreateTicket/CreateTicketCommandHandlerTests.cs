using FluentAssertions;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.RequestEscalation;
using Helpdesk.Api.Features.Tickets.CreateTicket;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Tickets.CreateTicket
{
    public class CreateTicketCommandHandlerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly HelpdeskDbContext _context;
        private readonly CreateTicketCommandHandler _handler;

        public CreateTicketCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);

            _userManagerMock = IdentityMockFactory.CreateUserManager();

            _handler = new CreateTicketCommandHandler(
                _context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Handle_ShouldCreateTicketAndReturnItsId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var command = new CreateTicketCommand(
                userId,
                "Cannot log in",
                "I am unable to access my account.",
                TicketPriority.High);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeEmpty();

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == result.Value);

            ticket.Should().NotBeNull();
            ticket!.Title.Should().Be("Cannot log in");
            ticket.Description.Should().Be("I am unable to access my account.");
            ticket.CreatedById.Should().Be(userId);
            ticket.Priority.Should().Be(TicketPriority.High);
        }

        [Fact]
        public async Task Handle_ShouldPersistTicketToDatabase()
        {
            // Arrange
            var command = new CreateTicketCommand(
                Guid.NewGuid(),
                "Printer not working",
                "The office printer is displaying an error.",
                TicketPriority.Medium);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var tickets = await _context.Tickets.ToListAsync();

            tickets.Should().ContainSingle();
        }

        [Fact]
        public async Task Handle_ShouldCreateTicketWithOpenStatus()
        {
            // Arrange
            var command = new CreateTicketCommand(
                Guid.NewGuid(),
                "Software issue",
                "Application crashes when opening.",
                TicketPriority.Low);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            var ticket = await _context.Tickets.FirstAsync(t => t.Id == result.Value);

            ticket.Status.Should().Be(TicketStatus.Open);
        }

        [Fact]
        public async Task Handle_ShouldCreateInitialTicketActivity()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var command = new CreateTicketCommand(
                userId,
                "Email problem",
                "Emails are not being received.",
                TicketPriority.Medium);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            var ticket = await _context.Tickets
                .Include(t => t.Activities)
                .FirstAsync(t => t.Id == result.Value);

            ticket.Activities.Should().ContainSingle();

            ticket.Activities.First().EventType.Should().Be(TicketActivityType.Created);
        }
    }
}
