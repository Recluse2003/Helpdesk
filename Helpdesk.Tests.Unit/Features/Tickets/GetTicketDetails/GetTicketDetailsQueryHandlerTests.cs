using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.GetTicketDetails;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Helpdesk.Tests.Unit.Features.Tickets.GetTicketDetails
{
    public class GetTicketDetailsQueryHandlerTests
    {
        private readonly HelpdeskDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly GetTicketDetailsQueryHandler _handler;

        public GetTicketDetailsQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);

            _userManagerMock = IdentityMockFactory.CreateUserManager();

            _handler = new GetTicketDetailsQueryHandler(
                _context,
                _userManagerMock.Object);
        }

        private static ApplicationUser CreateUser()
        {
            return new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@example.com",
                DisplayName = "Test User"
            };
        }

        private static Ticket CreateTicket(Guid customerId)
        {
            return new Ticket(
                "Test ticket",
                "Test description",
                customerId);
        }

        private void SetupUser(ApplicationUser user, bool isCustomer, bool isSupportAgent, bool isAdministrator)
        {
            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, RoleNames.Customer))
                .ReturnsAsync(isCustomer);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, RoleNames.SupportAgent))
                .ReturnsAsync(isSupportAgent);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, RoleNames.Administrator))
                .ReturnsAsync(isAdministrator);
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var query = new GetTicketDetailsQuery(
                Guid.NewGuid(),
                Guid.NewGuid());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(query.UserId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("User.NotFound");
        }

        [Fact]
        public async Task Handle_WhenUserHasNoAuthorisedRole_ReturnsUnauthorized()
        {
            // Arrange
            var user = CreateUser();
            var query = new GetTicketDetailsQuery(user.Id, Guid.NewGuid());

            SetupUser(user, false, false, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("User.Unauthorised");
        }

        [Fact]
        public async Task Handle_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var user = CreateUser();
            var query = new GetTicketDetailsQuery(user.Id, Guid.NewGuid());

            SetupUser(user, true, false, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Ticket.NotFound");
        }

        [Fact]
        public async Task Handle_WhenCustomerRequestsAnotherCustomersTicket_ReturnsNotFound()
        {
            // Arrange
            var customer = CreateUser();
            var anotherCustomer = CreateUser();

            var ticket = CreateTicket(anotherCustomer.Id);

            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketDetailsQuery(customer.Id, ticket.Id);

            SetupUser(customer, true, false, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Id.Should().Be("Ticket.NotFound");
        }

        [Fact]
        public async Task Handle_WhenCustomerRequestsOwnTicket_ReturnsTicketDetails()
        {
            // Arrange
            var customer = CreateUser();
            var ticket = CreateTicket(customer.Id);

            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketDetailsQuery(customer.Id, ticket.Id);

            SetupUser(customer, true, false, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(ticket.Id);
            result.Value.Title.Should().Be(ticket.Title);
            result.Value.Description.Should().Be(ticket.Description);
        }

        [Fact]
        public async Task Handle_WhenCustomerRequestsOwnTicket_ReturnsOnlyPublicMessagesAndActivities()
        {
            // Arrange
            var customer = CreateUser();
            var agent = CreateUser();

            var ticket = CreateTicket(customer.Id);

            // Add public and internal messages.
            ticket.AddPublicMessage("Public customer message", customer.Id);

            // Assign the ticket so the agent can add an internal note.
            ticket.Claim(agent.Id, agent.DisplayName);

            ticket.AddInternalNote(
                "Private internal note",
                agent.Id);

            // Add public and private activities.
            var publicActivity = new TicketActivity(
                ticket.Id,
                TicketActivityType.PriorityChanged,
                agent.Id,
                "Public activity",
                isPublic: true);

            var privateActivity = new TicketActivity(
                ticket.Id,
                TicketActivityType.PriorityChanged,
                agent.Id,
                "Private activity",
                isPublic: false);

            ticket.AddActivity(publicActivity);
            ticket.AddActivity(privateActivity);

            await _context.Users.AddRangeAsync(customer, agent);
            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketDetailsQuery(customer.Id, ticket.Id);

            SetupUser(customer, true, false, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Timeline.Should().Contain(x => x.Content == "Public customer message");

            result.Value.Timeline.Should().NotContain(x => x.Content == "Private internal note");

            result.Value.Timeline.Should().Contain(x => x.Details == "Public activity");

            result.Value.Timeline.Should().NotContain(x => x.Details == "Private activity");
        }

        [Fact]
        public async Task Handle_WhenSupportAgentRequestsTicket_IncludesInternalContent()
        {
            // Arrange
            var agent = CreateUser();
            var customer = CreateUser();

            var ticket = CreateTicket(customer.Id);

            ticket.Claim(agent.Id, agent.DisplayName);

            ticket.AddPublicMessage(
                "Public customer message",
                customer.Id);

            ticket.AddInternalNote(
                "Private internal note",
                agent.Id);

            var publicActivity = new TicketActivity(
                ticket.Id,
                TicketActivityType.PriorityChanged,
                agent.Id,
                "Public activity",
                isPublic: true);

            var privateActivity = new TicketActivity(
                ticket.Id,
                TicketActivityType.PriorityChanged,
                agent.Id,
                "Private activity",
                isPublic: false);

            ticket.AddActivity(publicActivity);
            ticket.AddActivity(privateActivity);

            await _context.Users.AddRangeAsync(agent, customer);
            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketDetailsQuery(agent.Id, ticket.Id);

            SetupUser(agent, false, true, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Timeline.Should().Contain(x => x.Content == "Public customer message");

            result.Value.Timeline.Should().Contain(x => x.Content == "Private internal note");

            result.Value.Timeline.Should().Contain(x => x.Details == "Public activity");

            result.Value.Timeline.Should().Contain(x => x.Details == "Private activity");
        }

        [Fact]
        public async Task Handle_WhenAdministratorRequestsTicket_IncludesInternalContent()
        {
            // Arrange
            var admin = CreateUser();
            var customer = CreateUser();
            var ticket = CreateTicket(customer.Id);

            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketDetailsQuery(admin.Id, ticket.Id);

            SetupUser(admin, false, false, true);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(ticket.Id);
        }

        [Fact]
        public async Task Handle_WhenActivitiesHaveNoActor_UsesSystemAsActorName()
        {
            // Arrange
            var customer = CreateUser();
            var ticket = CreateTicket(customer.Id);

            var systemActivity = new TicketActivity(
                ticket.Id,
                TicketActivityType.PriorityChanged,
                null,
                "System-generated activity");

            ticket.AddActivity(systemActivity);

            await _context.Users.AddAsync(customer);
            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketDetailsQuery(customer.Id, ticket.Id);

            SetupUser(customer, true, false, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Timeline.Should().Contain(
                x => x.Details == "System-generated activity" &&
                     x.ActorName == "System");
        }

        [Fact]
        public async Task Handle_WhenActivityActorDoesNotExist_UsesSystemAsActorName()
        {
            // Arrange
            var customer = CreateUser();
            var ticket = CreateTicket(customer.Id);

            var deletedUserId = Guid.NewGuid();

            var activity = new TicketActivity(
                ticket.Id,
                TicketActivityType.PriorityChanged,
                deletedUserId,
                "Activity by deleted user");

            ticket.AddActivity(activity);

            await _context.Users.AddAsync(customer);
            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketDetailsQuery(customer.Id, ticket.Id);

            SetupUser(customer, true, false, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Timeline.Should().Contain(
                x => x.Details == "Activity by deleted user" &&
                     x.ActorName == "System");
        }

        [Fact]
        public async Task Handle_WhenTicketHasMessagesAndActivities_ReturnsChronologicalTimeline()
        {
            // Arrange
            var customer = CreateUser();
            var ticket = CreateTicket(customer.Id);

            ticket.AddPublicMessage("First message", customer.Id);

            ticket.AddActivity(new TicketActivity(
                ticket.Id,
                TicketActivityType.PriorityChanged,
                customer.Id,
                "Second activity"));

            ticket.AddPublicMessage("Third message", customer.Id);

            await _context.Users.AddAsync(customer);
            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            var query = new GetTicketDetailsQuery(customer.Id, ticket.Id);

            SetupUser(customer, true, false, false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            result.Value.Timeline.Select(x => x.CreatedAt).Should().BeInAscendingOrder();
        }
    }
}
