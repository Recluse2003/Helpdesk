using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Agents.GetAgentDashboard;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

public class GetAgentDashboardQueryHandlerTests
{
    private readonly HelpdeskDbContext _context;
    private readonly GetAgentDashboardQueryHandler _handler;

    public GetAgentDashboardQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        _context = new HelpdeskDbContext(options);

        _handler = new GetAgentDashboardQueryHandler(_context);
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

    private async Task<Ticket> CreateTicketAsync(Guid? assignedAgentId = null, string ticketTitle = "Test ticket")
    {
        var ticket = new Ticket(
            ticketTitle,
            "Test ticket description",
            Guid.NewGuid(),
            TicketPriority.Medium);

        if (assignedAgentId.HasValue)
            ticket.Claim(assignedAgentId.Value, "Previous Agent");

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        return ticket;
    }

    [Fact]
    public async Task Handle_WhenAgentHasTickets_ReturnsCorrectAssignedTicketCount()
    {
        // Arrange
        var agent = await CreateUser();
        var customer = await CreateUser();

        var assignedTicket = await CreateTicketAsync(customer.Id, "Assigned ticket");
        assignedTicket.Claim(agent.Id, agent.DisplayName);

        var resolvedTicket = await CreateTicketAsync(customer.Id, "Resolved ticket");
        resolvedTicket.Claim(agent.Id, agent.DisplayName);
        resolvedTicket.Resolve();

        var anotherAgentTicket = await CreateTicketAsync(customer.Id, "Another agent ticket");
        anotherAgentTicket.Claim(Guid.NewGuid(), "Another agent");

        await _context.Tickets.AddRangeAsync(
            assignedTicket,
            resolvedTicket,
            anotherAgentTicket);

        await _context.SaveChangesAsync();

        var query = new GetAgentDashboardQuery(agent.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AssignedTicketCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenUnassignedTicketsExist_ReturnsCorrectUnassignedTicketCount()
    {
        // Arrange
        var agent = await CreateUser();
        var customer = await CreateUser();

        var unassignedTicket = await CreateTicketAsync(customer.Id, "Unassigned ticket");

        var assignedTicket = await CreateTicketAsync(customer.Id, "Assigned ticket");
        assignedTicket.Claim(agent.Id, agent.DisplayName);

        var resolvedTicket = await CreateTicketAsync(customer.Id, "Resolved ticket");
        resolvedTicket.Resolve();

        await _context.Tickets.AddRangeAsync(
            unassignedTicket,
            assignedTicket,
            resolvedTicket);

        await _context.SaveChangesAsync();

        var query = new GetAgentDashboardQuery(agent.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.UnassignedTicketCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenAgentHasNoTickets_ReturnsEmptyDashboardCollections()
    {
        // Arrange
        var agent = await CreateUser();
        var query = new GetAgentDashboardQuery(agent.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.AssignedTicketCount.Should().Be(0);
        result.Value.UnassignedTicketCount.Should().Be(0);
        result.Value.AwaitingCustomerResponseCount.Should().Be(0);

        result.Value.TicketsNeedingAttention.Should().BeEmpty();
        result.Value.RecentActivities.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveTicketsAssignedToAgent()
    {
        // Arrange
        var agent = await CreateUser();
        var customer = await CreateUser();

        var ownTicket = await CreateTicketAsync(customer.Id, "My ticket");
        ownTicket.Claim(agent.Id, agent.DisplayName);

        var anotherAgentTicket = await CreateTicketAsync(customer.Id, "Other agent ticket");
        anotherAgentTicket.Claim(Guid.NewGuid(), "Another agent");

        var unassignedTicket = await CreateTicketAsync(customer.Id, "Unassigned ticket");

        var resolvedTicket = await CreateTicketAsync(customer.Id, "Resolved ticket");
        resolvedTicket.Claim(agent.Id, agent.DisplayName);
        resolvedTicket.Resolve();

        await _context.Tickets.AddRangeAsync(
            ownTicket,
            anotherAgentTicket,
            unassignedTicket,
            resolvedTicket);

        await _context.SaveChangesAsync();

        var query = new GetAgentDashboardQuery(agent.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.TicketsNeedingAttention.Should().ContainSingle();

        result.Value.TicketsNeedingAttention.First().Id.Should().Be(ownTicket.Id);
    }

    [Fact]
    public async Task Handle_ReturnsTicketsOrderedByPriorityThenCreationDate()
    {
        // Arrange
        var agent = await CreateUser();
        var customer = await CreateUser();

        var lowPriorityTicket = await CreateTicketAsync(customer.Id, "Low priority");
        lowPriorityTicket.Claim(agent.Id, agent.DisplayName);
        lowPriorityTicket.ChangePriority(TicketPriority.Low, agent.Id);

        var highPriorityTicket = await CreateTicketAsync(customer.Id, "High priority");
        highPriorityTicket.Claim(agent.Id, agent.DisplayName);
        highPriorityTicket.ChangePriority(TicketPriority.High, agent.Id);

        var mediumPriorityTicket = await CreateTicketAsync(customer.Id, "Medium priority");
        mediumPriorityTicket.Claim(agent.Id, agent.DisplayName);
        mediumPriorityTicket.ChangePriority(TicketPriority.Medium, agent.Id);

        await _context.Tickets.AddRangeAsync(
            lowPriorityTicket,
            highPriorityTicket,
            mediumPriorityTicket);

        await _context.SaveChangesAsync();

        var query = new GetAgentDashboardQuery(agent.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.TicketsNeedingAttention
            .Select(x => x.Id)
            .Should()
            .ContainInOrder(
                highPriorityTicket.Id,
                mediumPriorityTicket.Id,
                lowPriorityTicket.Id);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyFiveMostRecentActivities()
    {
        // Arrange
        var agent = await CreateUser();
        var customer = await CreateUser();

        var ticket = await CreateTicketAsync(customer.Id, "Test ticket");
        ticket.Claim(agent.Id, agent.DisplayName);

        await _context.Tickets.AddAsync(ticket);
        await _context.SaveChangesAsync();

        // Add six activities belonging to the assigned ticket.
        var activities = Enumerable.Range(1, 6)
            .Select(_ => new TicketActivity(
                ticket.Id,
                TicketActivityType.StatusChanged,
                agent.Id))
            .ToList();

        await _context.TicketActivities.AddRangeAsync(activities);
        await _context.SaveChangesAsync();

        var query = new GetAgentDashboardQuery(agent.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.RecentActivities.Should().HaveCount(5);

        result.Value.RecentActivities.Should().BeInDescendingOrder(x => x.CreatedAt);
    }
}
