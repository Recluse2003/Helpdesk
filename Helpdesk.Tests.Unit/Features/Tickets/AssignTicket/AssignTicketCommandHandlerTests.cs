using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.AssignTicket;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Tickets.AssignTicket;

public class AssignTicketCommandHandlerTests
{
    private readonly HelpdeskDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly AssignTicketCommandHandler _handler;

    public AssignTicketCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new HelpdeskDbContext(options);

        _userManagerMock = IdentityMockFactory.CreateUserManager();

        _handler = new AssignTicketCommandHandler(
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

    private async Task<Ticket> CreateTicketAsync(Guid? assignedAgentId = null)
    {
        var ticket = new Ticket(
            "Test ticket",
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
    public async Task Handle_WhenAdministratorDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var command = new AssignTicketCommand(
            Guid.NewGuid(),
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
    public async Task Handle_WhenUserIsNotAdministrator_ReturnsForbidden()
    {
        // Arrange
        var user = await CreateUser("Test User");
        SetupUser(user, false, false);

        var command = new AssignTicketCommand(
            user.Id,
            Guid.NewGuid(),
            Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Id.Should().Be("Ticket.AssignTicketForbidden");
    }

    [Fact]
    public async Task Handle_WhenAssigneeDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var administrator = await CreateUser("Administrator");
        SetupUser(administrator, false, true);

        var command = new AssignTicketCommand(
            administrator.Id,
            Guid.NewGuid(),
            Guid.NewGuid());

        _userManagerMock
            .Setup(x => x.FindByIdAsync(command.AssigneeId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Id.Should().Be("Ticket.AssigneeNotFound");
    }

    [Fact]
    public async Task Handle_WhenAssigneeIsNotSupportAgent_ReturnsValidationError()
    {
        // Arrange
        var administrator = await CreateUser("Administrator");
        SetupUser(administrator, false, true);

        var assignee = await CreateUser("Customer");
        SetupUser(assignee, false, false);

        var command = new AssignTicketCommand(
            administrator.Id,
            Guid.NewGuid(),
            assignee.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Id.Should().Be("Ticket.InvalidAssignee");
    }

    [Fact]
    public async Task Handle_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var administrator = await CreateUser("Administrator");
        SetupUser(administrator, false, true);

        var assignee = await CreateUser("Support Agent");
        SetupUser(assignee, true, false);

        var command = new AssignTicketCommand(
            administrator.Id,
            Guid.NewGuid(),
            assignee.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Id.Should().Be("Ticket.NotFound");
    }

    [Fact]
    public async Task Handle_WhenValidAssignmentIsMade_AssignsTicketSuccessfully()
    {
        // Arrange
        var administrator = await CreateUser("Administrator");
        SetupUser(administrator, false, true);

        var assignee = await CreateUser("Support Agent");
        SetupUser(assignee, true, false);

        var ticket = await CreateTicketAsync();

        var command = new AssignTicketCommand(
            administrator.Id,
            ticket.Id,
            assignee.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedTicket = await _context.Tickets.FirstAsync(x => x.Id == ticket.Id);

        updatedTicket.AssignedToId.Should().Be(assignee.Id);
    }

    [Fact]
    public async Task Handle_WhenTicketAlreadyHasAssignee_ReassignsTicketSuccessfully()
    {
        // Arrange
        var administrator = await CreateUser("Administrator");
        SetupUser(administrator, false, true);

        var newAssignee = await CreateUser("New Support Agent");
        SetupUser(newAssignee, true, false);

        var previousAssignee = await CreateUser("Old Support Agent");
        SetupUser(previousAssignee, true, false);

        var ticket = await CreateTicketAsync(previousAssignee.Id);

        var command = new AssignTicketCommand(
            administrator.Id,
            ticket.Id,
            newAssignee.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedTicket = await _context.Tickets.FirstAsync(x => x.Id == ticket.Id);

        updatedTicket.AssignedToId.Should().Be(newAssignee.Id);
    }
}