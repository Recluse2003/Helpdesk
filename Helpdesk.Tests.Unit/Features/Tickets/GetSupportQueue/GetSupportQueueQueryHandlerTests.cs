using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.GetSupportQueue;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Tickets.GetSupportQueue
{
    public class GetSupportQueueQueryHandlerTests
    {
        private readonly HelpdeskDbContext _context;
        private readonly GetSupportQueueQueryHandler _handler;

        public GetSupportQueueQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HelpdeskDbContext(options);
            _handler = new GetSupportQueueQueryHandler(_context);
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

        private async Task<Ticket> CreateTicketAsync(
            Guid customerId,
            string title = "Test ticket",
            TicketPriority priority = TicketPriority.Medium,
            Guid? assignedAgentId = null)
        {
            var ticket = new Ticket(
                title,
                "Test ticket description.",
                customerId,
                priority);

            if (assignedAgentId.HasValue)
                ticket.Claim(assignedAgentId.Value, "Test Agent");

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return ticket;
        }

        [Fact]
        public async Task Handle_WhenTicketsExist_ReturnsAllTickets()
        {
            // Arrange
            var customer = await CreateUser("Customer");
            var agent = await CreateUser("Support Agent");

            await CreateTicketAsync(customer.Id, "Ticket 1");
            await CreateTicketAsync(customer.Id, "Ticket 2");

            var query = new GetSupportQueueQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WhenNoTicketsExist_ReturnsEmptyPagedList()
        {
            // Arrange
            var query = new GetSupportQueueQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
            result.Value.TotalPages.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenFilteringByStatus_ReturnsOnlyMatchingTickets()
        {
            // Arrange
            var customer = await CreateUser("Customer");
            var agent = await CreateUser("Support Agent");

            await CreateTicketAsync(customer.Id, "Open ticket");

            var inProgressTicket = await CreateTicketAsync(
                customer.Id,
                "In progress ticket");

            inProgressTicket.StartProgress();
            await _context.SaveChangesAsync();

            var query = new GetSupportQueueQuery(Status: TicketStatus.InProgress);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Items.Should().ContainSingle();
            result.Value.Items.Single().Title.Should().Be("In progress ticket");
        }

        [Fact]
        public async Task Handle_WhenFilteringByPriority_ReturnsOnlyMatchingTickets()
        {
            // Arrange
            var customer = await CreateUser("Customer");

            await CreateTicketAsync(
                customer.Id,
                "Low priority ticket",
                TicketPriority.Low);

            await CreateTicketAsync(
                customer.Id,
                "High priority ticket",
                TicketPriority.High);

            var query = new GetSupportQueueQuery(
                Priority: TicketPriority.High);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Items.Should().ContainSingle();
            result.Value.Items.Single().Title.Should().Be("High priority ticket");
        }

        [Fact]
        public async Task Handle_WhenFilteringByCustomer_ReturnsOnlyCustomerTickets()
        {
            // Arrange
            var customer1 = await CreateUser("Customer One");
            var customer2 = await CreateUser("Customer Two");

            await CreateTicketAsync(customer1.Id, "Customer One ticket");
            await CreateTicketAsync(customer2.Id, "Customer Two ticket");

            var query = new GetSupportQueueQuery(
                CustomerId: customer1.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Items.Should().ContainSingle();
            result.Value.Items.Single().CreatedById.Should().Be(customer1.Id);
            result.Value.Items.Single().CreatedByDisplayName.Should().Be("Customer One");
        }

        [Fact]
        public async Task Handle_WhenFilteringByAssignee_ReturnsOnlyAssignedTickets()
        {
            // Arrange
            var customer = await CreateUser("Customer");
            var agent1 = await CreateUser("Agent One");
            var agent2 = await CreateUser("Agent Two");

            await CreateTicketAsync(
                customer.Id,
                "Agent One ticket",
                assignedAgentId: agent1.Id);

            await CreateTicketAsync(
                customer.Id,
                "Agent Two ticket",
                assignedAgentId: agent2.Id);

            var query = new GetSupportQueueQuery(
                AssigneeId: agent1.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Items.Should().ContainSingle();

            var ticket = result.Value.Items.Single();
            ticket.Title.Should().Be("Agent One ticket");
            ticket.AssignedToId.Should().Be(agent1.Id);
            ticket.AssigneeDisplayName.Should().Be("Agent One");
        }

        [Fact]
        public async Task Handle_WhenTicketIsUnassigned_IncludesTicketWithNullAssignee()
        {
            // Arrange
            var customer = await CreateUser("Customer");

            await CreateTicketAsync(customer.Id, "Unassigned ticket");

            var query = new GetSupportQueueQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var ticket = result.Value.Items.Single();

            ticket.AssignedToId.Should().BeNull();
            ticket.AssigneeDisplayName.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WhenTicketIsAssigned_ReturnsCustomerAndAssigneeNames()
        {
            // Arrange
            var customer = await CreateUser("Customer");
            var agent = await CreateUser("Support Agent");

            await CreateTicketAsync(
                customer.Id,
                assignedAgentId: agent.Id);

            var query = new GetSupportQueueQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var ticket = result.Value.Items.Single();

            ticket.CreatedByDisplayName.Should().Be("Customer");
            ticket.AssigneeDisplayName.Should().Be("Support Agent");
        }

        [Fact]
        public async Task Handle_WhenMultipleFiltersAreProvided_AppliesAllFilters()
        {
            // Arrange
            var customer1 = await CreateUser("Customer One");
            var customer2 = await CreateUser("Customer Two");
            var agent = await CreateUser("Support Agent");

            await CreateTicketAsync(
                customer1.Id,
                "Matching ticket",
                TicketPriority.High,
                agent.Id);

            await CreateTicketAsync(
                customer1.Id,
                "Wrong priority",
                TicketPriority.Low,
                agent.Id);

            await CreateTicketAsync(
                customer2.Id,
                "Wrong customer",
                TicketPriority.High,
                agent.Id);

            var query = new GetSupportQueueQuery(
                Priority: TicketPriority.High,
                CustomerId: customer1.Id,
                AssigneeId: agent.Id);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Items.Should().ContainSingle();
            result.Value.Items.Single().Title.Should().Be("Matching ticket");
        }

        [Fact]
        public async Task Handle_WhenPageSizeIsExceeded_ReturnsOnlyRequestedPageSize()
        {
            // Arrange
            var customer = await CreateUser("Customer");

            for (int i = 1; i <= 5; i++)
                await CreateTicketAsync(customer.Id, $"Ticket {i}");

            var query = new GetSupportQueueQuery(
                PageNumber: 1,
                PageSize: 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Items.Should().HaveCount(2);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(2);
            result.Value.TotalCount.Should().Be(5);
            result.Value.TotalPages.Should().Be(3);
            result.Value.HasNextPage.Should().BeTrue();
            result.Value.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_WhenRequestingSecondPage_ReturnsCorrectPage()
        {
            // Arrange
            var customer = await CreateUser("Customer");

            for (int i = 1; i <= 5; i++)
                await CreateTicketAsync(customer.Id, $"Ticket {i}");

            var query = new GetSupportQueueQuery(
                PageNumber: 2,
                PageSize: 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Items.Should().HaveCount(2);
            result.Value.PageNumber.Should().Be(2);
            result.Value.TotalCount.Should().Be(5);
            result.Value.HasNextPage.Should().BeTrue();
            result.Value.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WhenPageNumberIsLessThanOne_UsesFirstPage()
        {
            // Arrange
            var customer = await CreateUser("Customer");

            await CreateTicketAsync(customer.Id);

            var query = new GetSupportQueueQuery(
                PageNumber: 0,
                PageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.PageNumber.Should().Be(1);
            result.Value.Items.Should().ContainSingle();
        }

        [Fact]
        public async Task Handle_WhenPageSizeIsLessThanOne_UsesDefaultPageSize()
        {
            // Arrange
            var customer = await CreateUser("Customer");

            var query = new GetSupportQueueQuery(
                PageNumber: 1,
                PageSize: 0);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.PageSize.Should().Be(10);
        }
    }
}
