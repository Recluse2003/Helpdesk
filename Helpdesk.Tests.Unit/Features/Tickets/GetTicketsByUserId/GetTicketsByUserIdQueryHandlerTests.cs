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
        public async Task Handle_WhenUserHasTickets_ReturnsTheirTicketsAndStatistics()
        {
            // Arrange
            var customer = await CreateUser();

            var ticket1 = CreateTicket(customer.Id, "First ticket");
            var ticket2 = CreateTicket(customer.Id, "Second ticket");

            await _context.Tickets.AddRangeAsync(ticket1, ticket2);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 10, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().HaveCount(2);
            result.Value.Tickets.TotalCount.Should().Be(2);

            result.Value.Statistics.TotalTickets.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WhenUserHasNoTickets_ReturnsEmptyListAndZeroStatistics()
        {
            // Arrange
            var customer = await CreateUser();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 10, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().BeEmpty();
            result.Value.Tickets.TotalCount.Should().Be(0);

            result.Value.Statistics.TotalTickets.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenOtherUsersHaveTickets_ReturnsOnlyRequestingUsersTickets()
        {
            // Arrange
            var customer = await CreateUser("Customer");
            var anotherCustomer = await CreateUser("Another Customer");

            var ownTicket = CreateTicket(customer.Id, "My ticket");
            var otherTicket = CreateTicket(anotherCustomer.Id, "Another user's ticket");

            await _context.Tickets.AddRangeAsync(ownTicket, otherTicket);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 10, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().ContainSingle();
            result.Value.Tickets.Items.Should().Contain(x => x.Title == "My ticket");
            result.Value.Tickets.Items.Should().NotContain(x => x.Title == "Another user's ticket");

            result.Value.Statistics.TotalTickets.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WhenTicketIsAssigned_ReturnsAssignedAgentDisplayName()
        {
            // Arrange
            var customer = await CreateUser("Customer");
            var agent = await CreateUser("Support Agent");

            var ticket = CreateTicket(customer.Id, "Assigned ticket");
            ticket.Claim(agent.Id, agent.DisplayName);

            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 10, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var returnedTicket = result.Value.Tickets.Items.Should().ContainSingle().Subject;

            returnedTicket.AssignedAgentUsername.Should().Be(agent.DisplayName);
            returnedTicket.AssignedAgentId.Should().Be(agent.Id);
        }

        [Fact]
        public async Task Handle_WhenTicketIsUnassigned_ReturnsNullAssignedAgentDetails()
        {
            // Arrange
            var customer = await CreateUser();

            var ticket = CreateTicket(customer.Id, "Unassigned ticket");

            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 10, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var returnedTicket = result.Value.Tickets.Items.Should().ContainSingle().Subject;

            returnedTicket.AssignedAgentUsername.Should().BeNull();
            returnedTicket.AssignedAgentId.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WhenFilteringByPageSize_ReturnsOnlyRequestedNumberOfTickets()
        {
            // Arrange
            var customer = await CreateUser();

            var tickets = Enumerable.Range(1, 5)
                .Select(i => CreateTicket(customer.Id, $"Ticket {i}"))
                .ToList();

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 2, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().HaveCount(2);
            result.Value.Tickets.PageNumber.Should().Be(1);
            result.Value.Tickets.PageSize.Should().Be(2);
            result.Value.Tickets.TotalCount.Should().Be(5);
            result.Value.Tickets.TotalPages.Should().Be(3);

            result.Value.Statistics.TotalTickets.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WhenRequestingSecondPage_ReturnsRemainingTickets()
        {
            // Arrange
            var customer = await CreateUser();

            var tickets = Enumerable.Range(1, 5)
                .Select(i => CreateTicket(customer.Id, $"Ticket {i}"))
                .ToList();

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 2, 2, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().HaveCount(2);
            result.Value.Tickets.PageNumber.Should().Be(2);
            result.Value.Tickets.PageSize.Should().Be(2);
            result.Value.Tickets.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WhenRequestingLastPage_ReturnsRemainingTicket()
        {
            // Arrange
            var customer = await CreateUser();

            var tickets = Enumerable.Range(1, 5)
                .Select(i => CreateTicket(customer.Id, $"Ticket {i}"))
                .ToList();

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 3, 2, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().ContainSingle();
            result.Value.Tickets.PageNumber.Should().Be(3);
            result.Value.Tickets.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WhenPageNumberIsLessThanOne_UsesFirstPage()
        {
            // Arrange
            var customer = await CreateUser();

            var tickets = Enumerable.Range(1, 3)
                .Select(i => CreateTicket(customer.Id, $"Ticket {i}"))
                .ToList();

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 0, 2, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.PageNumber.Should().Be(1);
            result.Value.Tickets.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_WhenPageSizeIsLessThanOne_UsesDefaultPageSize()
        {
            // Arrange
            var customer = await CreateUser();

            var tickets = Enumerable.Range(1, 12)
                .Select(i => CreateTicket(customer.Id, $"Ticket {i}"))
                .ToList();

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 0, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.PageNumber.Should().Be(1);
            result.Value.Tickets.PageSize.Should().Be(10);
            result.Value.Tickets.Items.Should().HaveCount(10);
            result.Value.Tickets.TotalCount.Should().Be(12);
        }

        [Fact]
        public async Task Handle_WhenSearchTermIsSpecified_ReturnsMatchingTickets()
        {
            // Arrange
            var customer = await CreateUser();

            await _context.Tickets.AddRangeAsync(
                CreateTicket(customer.Id, "Password reset"),
                CreateTicket(customer.Id, "Unable to login"),
                CreateTicket(customer.Id, "Password recovery")
            );

            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 10, "password");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().HaveCount(2);
            result.Value.Tickets.Items.Should().OnlyContain(
                x => x.Title.Contains("password", StringComparison.OrdinalIgnoreCase));

            result.Value.Tickets.TotalCount.Should().Be(2);
            result.Value.Statistics.TotalTickets.Should().Be(3);
        }

        [Fact]
        public async Task Handle_WhenSearchTermHasNoMatches_ReturnsEmptyResults()
        {
            // Arrange
            var customer = await CreateUser();

            await _context.Tickets.AddRangeAsync(
                CreateTicket(customer.Id, "Password reset"),
                CreateTicket(customer.Id, "Unable to login")
            );

            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 10, "printer");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().BeEmpty();
            result.Value.Tickets.TotalCount.Should().Be(0);
            result.Value.Statistics.TotalTickets.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WhenSearchTermIsSpecifiedAndMultiplePagesExist_ReturnsCorrectPage()
        {
            // Arrange
            var customer = await CreateUser();

            var tickets = Enumerable.Range(1, 5)
                .Select(i => CreateTicket(customer.Id, $"Password issue {i}"))
                .ToList();

            tickets.Add(CreateTicket(customer.Id, "Printer issue"));

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 2, 2, "password");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().HaveCount(2);
            result.Value.Tickets.PageNumber.Should().Be(2);
            result.Value.Tickets.PageSize.Should().Be(2);
            result.Value.Tickets.TotalCount.Should().Be(5);
            result.Value.Tickets.TotalPages.Should().Be(3);

            result.Value.Statistics.TotalTickets.Should().Be(6);
        }

        [Fact]
        public async Task Handle_WhenSearchTermIsEmpty_ReturnsAllUserTickets()
        {
            // Arrange
            var customer = await CreateUser();

            await _context.Tickets.AddRangeAsync(
                CreateTicket(customer.Id, "First ticket"),
                CreateTicket(customer.Id, "Second ticket")
            );

            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 10, "");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().HaveCount(2);
            result.Value.Tickets.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WhenTicketsHaveDifferentUpdatedDates_ReturnsNewestFirst()
        {
            // Arrange
            var customer = await CreateUser();

            var olderTicket = CreateTicket(customer.Id, "Older ticket");
            var newerTicket = CreateTicket(customer.Id, "Newer ticket");

            await _context.Tickets.AddRangeAsync(olderTicket, newerTicket);
            await _context.SaveChangesAsync();

            _context.Entry(olderTicket)
                .Property(x => x.UpdatedAt)
                .CurrentValue = DateTime.UtcNow.AddDays(-1);

            _context.Entry(newerTicket)
                .Property(x => x.UpdatedAt)
                .CurrentValue = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(customer.Id, 1, 10, null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items
                .Select(x => x.Title)
                .Should()
                .ContainInOrder("Newer ticket", "Older ticket");
        }

        [Fact]
        public async Task Handle_WhenSearchTermAndStatusAreSpecified_ReturnsMatchingTickets()
        {
            // Arrange
            var customer = await CreateUser();

            var matchingTicket = CreateTicket(customer.Id, "Password reset");
            var otherMatchingTicket = CreateTicket(customer.Id, "Password recovery");
            var unrelatedTicket = CreateTicket(customer.Id, "Printer issue");

            matchingTicket.Resolve();
            otherMatchingTicket.Resolve();

            await _context.Tickets.AddRangeAsync(
                matchingTicket,
                otherMatchingTicket,
                unrelatedTicket
            );

            await _context.SaveChangesAsync();

            var query = new GetTicketsByUserIdQuery(
                customer.Id,
                1,
                10,
                "password",
                TicketStatus.Resolved
            );

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Tickets.Items.Should().HaveCount(2);
            result.Value.Tickets.Items.Should().OnlyContain(
                x => x.Status == TicketStatus.Resolved &&
                     x.Title.Contains("password", StringComparison.OrdinalIgnoreCase)
            );
        }
    }
}
