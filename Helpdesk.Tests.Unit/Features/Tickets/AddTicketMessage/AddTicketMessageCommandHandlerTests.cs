
using Castle.Core.Resource;
using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Domain.Exceptions;
using Helpdesk.Api.Features.Tickets.AddTicketMessage;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Tickets.AddTicketMessage;

public class AddTicketMessageCommandHandlerTests
{
    private readonly HelpdeskDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly AddTicketMessageCommandHandler _handler;

    public AddTicketMessageCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new HelpdeskDbContext(options);

        _userManagerMock = IdentityMockFactory.CreateUserManager();

        _handler = new AddTicketMessageCommandHandler(
            _context,
            _userManagerMock.Object);
    }

    private async Task<Ticket> CreateTicketAsync(Guid? assignedAgentId = null, Guid? creatorId = null)
    {
        creatorId ??= Guid.NewGuid();

        var ticket = new Ticket(
            "Ticket title",
            "Ticket description.",
            creatorId.Value,
            TicketPriority.Medium);

        if (assignedAgentId.HasValue)
        {
            ticket.Claim(assignedAgentId.Value, "Test Agent");
        }

        _context.Tickets.Add(ticket);

        await _context.SaveChangesAsync();

        return ticket;
    }

    private static ApplicationUser CreateUser(string displayName = "Test User")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"user-{Guid.NewGuid()}@example.com",
            Email = $"user-{Guid.NewGuid()}@example.com",
            DisplayName = displayName
        };
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

    [Fact]
    public async Task Handle_WhenTicketDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var command = new AddTicketMessageCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test message",
            true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Id.Should().Be("Ticket.NotFound");

        _userManagerMock.Verify(
            x => x.FindByIdAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAddingPublicMessage_CreatesPublicMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticket = await CreateTicketAsync(creatorId: userId);

        var command = new AddTicketMessageCommand(
            userId,
            ticket.Id,
            "This is a public message.",
            true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var message = ticket.Messages.Should().ContainSingle().Which;

        message.Content.Should().Be("This is a public message.");
        message.IsPublic.Should().BeTrue();

        _userManagerMock.Verify(
            x => x.FindByIdAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var ticket = await CreateTicketAsync();
        var userId = Guid.NewGuid();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        var command = new AddTicketMessageCommand(
            userId,
            ticket.Id,
            "Internal note.",
            false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Id.Should().Be("User.NotFound");

        ticket.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCustomerAttemptsInternalNote_ReturnsForbidden()
    {
        // Arrange
        var customer = CreateUser();
        SetupUser(customer, false, false);

        var ticket = await CreateTicketAsync();
        

        var command = new AddTicketMessageCommand(
            customer.Id,
            ticket.Id,
            "Customer internal note.",
            false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Id.Should().Be("Ticket.InternalNoteForbidden");

        ticket.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAssignedAgentAddsInternalNote_ReturnsSuccess()
    {
        // Arrange
        var agent = CreateUser("Support Agent");
        SetupUser(agent, true, false);

        var ticket = await CreateTicketAsync(agent.Id);

        var command = new AddTicketMessageCommand(
            agent.Id,
            ticket.Id,
            "Agent internal note.",
            false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var message = ticket.Messages.Should().ContainSingle().Which;

        message.Content.Should().Be("Agent internal note.");
        message.IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAdministratorAddsInternalNote_ReturnsSuccess()
    {
        // Arrange
        var administrator = CreateUser("Administrator");
        SetupUser(administrator, false, true);

        var assignedAgent = CreateUser("Assigned Agent");

        var ticket = await CreateTicketAsync(assignedAgent.Id);

        var command = new AddTicketMessageCommand(
            administrator.Id,
            ticket.Id,
            "Administrator internal note.",
            false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var message = ticket.Messages.Should().ContainSingle().Which;

        message.Content.Should().Be("Administrator internal note.");
        message.IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUnassignedAgentAddsInternalNote_ThrowsDomainException()
    {
        // Arrange
        var agent = CreateUser("Unassigned Agent");
        var ticket = await CreateTicketAsync();

        SetupUser(agent, true, false);

        var command = new AddTicketMessageCommand(
            agent.Id,
            ticket.Id,
            "Unassigned agent internal note.",
            false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();

        ticket.Messages.Should().BeEmpty();
    }
}
