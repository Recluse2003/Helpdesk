using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.ChangeTicketPriority;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Tickets.ChangeTicketPriority;

public class ChangeTicketPriorityCommandHandlerTests
{
    private readonly HelpdeskDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly ChangeTicketPriorityCommandHandler _handler;

    public ChangeTicketPriorityCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new HelpdeskDbContext(options);
        _userManagerMock = IdentityMockFactory.CreateUserManager();

        _handler = new ChangeTicketPriorityCommandHandler(
            _context,
            _userManagerMock.Object);
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

    private void SetupUser(ApplicationUser user, bool isSupportAgent, bool isAdministrator)
    {
        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsInRoleAsync(user, RoleNames.SupportAgent))
            .ReturnsAsync(isSupportAgent);

        _userManagerMock
            .Setup(x => x.IsInRoleAsync(user, RoleNames.Administrator))
            .ReturnsAsync(isAdministrator);
    }

    private async Task<Ticket> CreateTicketAsync(Guid? assignedAgentId = null,  TicketStatus status = TicketStatus.Open)
    {
        var ticket = new Ticket(
            "Test ticket",
            "Test ticket description.",
            Guid.NewGuid(),
            TicketPriority.Low);

        if (assignedAgentId.HasValue)
            ticket.Claim(assignedAgentId.Value, "Test Agent");

        if (status == TicketStatus.InProgress)
            ticket.StartProgress();

        if (status == TicketStatus.Resolved)
        {
            ticket.Resolve();
        }
        else if (status == TicketStatus.Closed)
        {
            ticket.Resolve();
            ticket.Close();
        }

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        return ticket;
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var command = new ChangeTicketPriorityCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TicketPriority.High);

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
    public async Task Handle_WhenUserIsNotSupportAgentOrAdministrator_ReturnsForbidden()
    {
        // Arrange
        var customer = await CreateUser("Customer");
        SetupUser(customer, false, false);

        var command = new ChangeTicketPriorityCommand(
            customer.Id,
            Guid.NewGuid(),
            TicketPriority.High);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Id.Should().Be("Ticket.PriorityChangeForbidden");
    }

    [Fact]
    public async Task Handle_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var agent = await CreateUser("Support Agent");
        SetupUser(agent, true, false);

        var command = new ChangeTicketPriorityCommand(
            agent.Id,
            Guid.NewGuid(),
            TicketPriority.High);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Id.Should().Be("Ticket.NotFound");
    }

    [Fact]
    public async Task Handle_WhenAgentIsNotAssignedToTicket_ReturnsForbidden()
    {
        // Arrange
        var agent = await CreateUser("Support Agent");
        SetupUser(agent, true, false);

        var assignedAgent = await CreateUser("Assigned Agent");

        var ticket = await CreateTicketAsync(assignedAgent.Id);

        var command = new ChangeTicketPriorityCommand(
            agent.Id,
            ticket.Id,
            TicketPriority.High);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Id.Should().Be("Ticket.PriorityChangeForbidden");

        var unchangedTicket = await _context.Tickets.FirstAsync(x => x.Id == ticket.Id);

        unchangedTicket.Priority.Should().Be(TicketPriority.Low);
    }

    [Fact]
    public async Task Handle_WhenTicketIsClosed_ReturnsConflict()
    {
        // Arrange
        var agent = await CreateUser("Support Agent");
        SetupUser(agent, true, false);

        var ticket = await CreateTicketAsync(agent.Id, TicketStatus.Closed);

        var command = new ChangeTicketPriorityCommand(
            agent.Id,
            ticket.Id,
            TicketPriority.High);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Id.Should().Be("Ticket.AlreadyClosed");

        var unchangedTicket = await _context.Tickets.FirstAsync(x => x.Id == ticket.Id);

        unchangedTicket.Priority.Should().Be(TicketPriority.Low);
    }

    [Fact]
    public async Task Handle_WhenAssignedAgentChangesPriority_ReturnsSuccess()
    {
        // Arrange
        var agent = await CreateUser("Support Agent");
        SetupUser(agent, true, false);

        var ticket = await CreateTicketAsync(agent.Id);

        var command = new ChangeTicketPriorityCommand(
            agent.Id,
            ticket.Id,
            TicketPriority.High);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedTicket = await _context.Tickets.FirstAsync(x => x.Id == ticket.Id);

        updatedTicket.Priority.Should().Be(TicketPriority.High);
    }

    [Fact]
    public async Task Handle_WhenAdministratorChangesUnassignedTicketPriority_ReturnsSuccess()
    {
        // Arrange
        var administrator = await CreateUser("Administrator");
        SetupUser(administrator, false, true);

        var ticket = await CreateTicketAsync();

        var command = new ChangeTicketPriorityCommand(
            administrator.Id,
            ticket.Id,
            TicketPriority.Urgent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedTicket = await _context.Tickets.FirstAsync(x => x.Id == ticket.Id);

        updatedTicket.Priority.Should().Be(TicketPriority.Urgent);
    }

    [Fact]
    public async Task Handle_WhenAdministratorChangesAnotherAgentsTicketPriority_ReturnsSuccess()
    {
        // Arrange
        var administrator = await CreateUser("Administrator");
        SetupUser(administrator, false, true);

        var assignedAgent = await CreateUser("Assigned agent");

        var ticket = await CreateTicketAsync(assignedAgent.Id);

        var command = new ChangeTicketPriorityCommand(
            administrator.Id,
            ticket.Id,
            TicketPriority.High);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedTicket = await _context.Tickets.FirstAsync(x => x.Id == ticket.Id);

        updatedTicket.Priority.Should().Be(TicketPriority.High);
    }
}