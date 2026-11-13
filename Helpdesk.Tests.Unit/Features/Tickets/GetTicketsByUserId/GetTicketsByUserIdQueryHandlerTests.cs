using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.GetTicketsByUserId;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Tests.Unit.Features.Tickets.GetTicketsByUserId
{
    public class GetTicketsByUserIdQueryHandlerTests
    {
        private readonly HelpdeskDbContext _context;
        private readonly GetTicketsByUserIdQueryHandler _handler;

        public GetTicketsByUserIdQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);
            _handler = new GetTicketsByUserIdQueryHandler(_context);
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

        private static Ticket CreateTicket(Guid customerId, string title)
        {
            return new Ticket(
                title,
                "Test ticket description",
                customerId);
        }

        [Fact]
        public async Task Handle_WhenUserHasTickets_ReturnsTheirTickets()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var ticket1 = CreateTicket(customerId, "First ticket");
            var ticket2 = CreateTicket(customerId, "Second ticket");

            await _context.Tickets.AddRangeAsync(ticket1, ticket2);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 1, 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WhenUserHasNoTickets_ReturnsEmptyList()
        {
            // Arrange
            var query = new GetTicketsByUserIdQuery(Guid.NewGuid(), 1, 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenOtherUsersHaveTickets_ReturnsOnlyRequestingUsersTickets()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var anotherCustomerId = Guid.NewGuid();

            var ownTicket = CreateTicket(customerId, "My ticket");
            var otherTicket = CreateTicket(anotherCustomerId, "Another user's ticket");

            await _context.Tickets.AddRangeAsync(ownTicket, otherTicket);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 1, 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().ContainSingle();
            result.Value.Items.Should().NotContain(x => x.Title == "Another user's ticket");
        }

        [Fact]
        public async Task Handle_WhenTicketIsAssigned_ReturnsAssignedAgentDisplayName()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var agent = await CreateUser("Support Agent");
            var ticket = CreateTicket(customerId, "Assigned ticket");

            ticket.Claim(agent.Id, agent.DisplayName);

            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 1, 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Items.Should().ContainSingle();
            result.Value.Items.First().AssignedAgentUsername.Should().Be(agent.DisplayName);
        }

        [Fact]
        public async Task Handle_WhenTicketIsUnassigned_ReturnsNullAssignedAgentName()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var ticket = CreateTicket(customerId, "Unassigned ticket");

            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 1, 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Items.Should().ContainSingle();
            result.Value.Items.First().AssignedAgentUsername.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WhenFilteringByPageSize_ReturnsOnlyRequestedNumberOfTickets()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var tickets = Enumerable.Range(1, 5)
                .Select(i => CreateTicket(customerId, $"Ticket {i}"))
                .ToList();

            _context.Tickets.AddRange(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 1, 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(2);
            result.Value.TotalCount.Should().Be(5);
            result.Value.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task Handle_WhenRequestingSecondPage_ReturnsRemainingTickets()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var tickets = Enumerable.Range(1, 5)
                .Select(i => CreateTicket(customerId, $"Ticket {i}"))
                .ToList();

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 2, 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(2);
            result.Value.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WhenRequestingLastPage_ReturnsRemainingTicket()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var tickets = Enumerable.Range(1, 5)
                .Select(i => CreateTicket(customerId, $"Ticket {i}"))
                .ToList();

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 3, 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().ContainSingle();
            result.Value.PageNumber.Should().Be(3);
            result.Value.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WhenPageNumberIsLessThanOne_UsesFirstPage()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var tickets = Enumerable.Range(1, 3)
                .Select(i => CreateTicket(customerId, $"Ticket {i}"))
                .ToList();

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 0, 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PageNumber.Should().Be(1);
            result.Value.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_WhenPageSizeIsLessThanOne_UsesDefaultPageSize()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var tickets = Enumerable.Range(1, 12)
                .Select(i => CreateTicket(customerId, $"Ticket {i}"))
                .ToList();

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 1, 0);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
            result.Value.Items.Should().HaveCount(10);
            result.Value.TotalCount.Should().Be(12);
        }

        [Fact]
        public async Task Handle_WhenTicketsHaveDifferentUpdatedDates_ReturnsNewestFirst()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var olderTicket = CreateTicket(customerId, "Older ticket");
            var newerTicket = CreateTicket(customerId, "Newer ticket");

            await _context.Tickets.AddRangeAsync(olderTicket, newerTicket);
            await _context.SaveChangesAsync();

            _context.Entry(olderTicket)
                .Property(x => x.UpdatedAt)
                .CurrentValue = DateTime.UtcNow.AddDays(-1);

            _context.Entry(newerTicket)
                .Property(x => x.UpdatedAt)
                .CurrentValue = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customerId, 1, 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Items.Select(x => x.Title).Should().ContainInOrder("Newer ticket", "Older ticket");
        }
    }
}
