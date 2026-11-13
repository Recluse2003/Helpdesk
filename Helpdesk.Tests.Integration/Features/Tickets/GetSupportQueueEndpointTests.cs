using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Helpdesk.Tests.Integration.Features.Tickets
{
    public class GetSupportQueueEndpointTests : IntegrationTestBase
    {
        public GetSupportQueueEndpointTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        private async Task<ApplicationUser> CreateUserAsync(string displayName, string role)
        {
            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = $"user-{Guid.NewGuid()}@example.com",
                Email = $"user-{Guid.NewGuid()}@example.com",
                DisplayName = displayName,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, "Password123!");

            var roleResult = await userManager.AddToRoleAsync(user, role);

            return user;
        }

        private async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
        {
            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

            var roles = await userManager.GetRolesAsync(user);

            return tokenService.GenerateAccessToken(
                user.Id,
                user.Email!,
                user.DisplayName,
                roles);
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

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            return ticket;
        }

        private void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task<HttpResponseMessage> GetSupportQueueAsync(string queryString = "")
        {
            return await Client.GetAsync($"/api/tickets/queue{queryString}");
        }

        private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content);
        }

        [Fact]
        public async Task GetSupportQueue_WhenUnauthenticated_ReturnsUnauthorized()
        {
            // Act
            var response = await GetSupportQueueAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetSupportQueue_WhenUserIsCustomer_ReturnsForbidden()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetSupportQueue_WhenUserIsSupportAgent_ReturnsOk()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var json = await ReadJsonAsync(response);

            json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);

            json.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
        }

        [Fact]
        public async Task GetSupportQueue_WhenUserIsAdministrator_ReturnsOk()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Administrator",
                RoleNames.Administrator);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetSupportQueue_WhenNoTicketsExist_ReturnsEmptyList()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var json = await ReadJsonAsync(response);

            json.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);

            json.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
        }

        [Fact]
        public async Task GetSupportQueue_ReturnsTicketCustomerAndAssigneeDetails()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            await CreateTicketAsync(
                customer.Id,
                "Assigned ticket",
                TicketPriority.High,
                agent.Id);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var json = await ReadJsonAsync(response);

            var item = json.RootElement.GetProperty("items")[0];

            item.GetProperty("title").GetString().Should().Be("Assigned ticket");

            item.GetProperty("createdByDisplayName").GetString().Should().Be("Customer");

            item.GetProperty("assigneeDisplayName").GetString().Should().Be("Support Agent");

            item.GetProperty("priority").GetInt32().Should().Be((int)TicketPriority.High);
        }

        [Fact]
        public async Task GetSupportQueue_WhenTicketIsUnassigned_ReturnsNullAssignee()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            await CreateTicketAsync(
                customer.Id,
                "Unassigned ticket");

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var json = await ReadJsonAsync(response);

            var item = json.RootElement.GetProperty("items")[0];

            item.GetProperty("assignedToId").ValueKind.Should().Be(JsonValueKind.Null);

            item.GetProperty("assigneeDisplayName").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Fact]
        public async Task GetSupportQueue_WhenFilteringByPriority_ReturnsMatchingTickets()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            await CreateTicketAsync(
                customer.Id,
                "Low priority ticket",
                TicketPriority.Low);

            await CreateTicketAsync(
                customer.Id,
                "High priority ticket",
                TicketPriority.High);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync(
                $"?Priority={(int)TicketPriority.High}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var json = await ReadJsonAsync(response);

            var items = json.RootElement.GetProperty("items");

            items.GetArrayLength().Should().Be(1);

            items[0].GetProperty("title").GetString().Should().Be("High priority ticket");
        }

        [Fact]
        public async Task GetSupportQueue_WhenFilteringByCustomer_ReturnsMatchingTickets()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var customer1 = await CreateUserAsync(
                "Customer One",
                RoleNames.Customer);

            var customer2 = await CreateUserAsync(
                "Customer Two",
                RoleNames.Customer);

            await CreateTicketAsync(customer1.Id, "Customer One ticket");
            await CreateTicketAsync(customer2.Id, "Customer Two ticket");

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync(
                $"?CustomerId={customer1.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var json = await ReadJsonAsync(response);

            var items = json.RootElement.GetProperty("items");

            items.GetArrayLength().Should().Be(1);

            items[0].GetProperty("createdById").GetGuid().Should().Be(customer1.Id);
        }

        [Fact]
        public async Task GetSupportQueue_WhenFilteringByAssignee_ReturnsMatchingTickets()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var otherAgent = await CreateUserAsync(
                "Other Agent",
                RoleNames.SupportAgent);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            await CreateTicketAsync(
                customer.Id,
                "Assigned to first agent",
                assignedAgentId: agent.Id);

            await CreateTicketAsync(
                customer.Id,
                "Assigned to second agent",
                assignedAgentId: otherAgent.Id);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync(
                $"?AssigneeId={agent.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var json = await ReadJsonAsync(response);

            var items = json.RootElement.GetProperty("items");

            items.GetArrayLength().Should().Be(1);

            items[0].GetProperty("title").GetString().Should().Be("Assigned to first agent");
        }

        [Fact]
        public async Task GetSupportQueue_WhenUsingPagination_ReturnsCorrectPage()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            for (int i = 1; i <= 5; i++)
                await CreateTicketAsync(customer.Id, $"Ticket {i}");

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync(
                "?PageNumber=2&PageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var json = await ReadJsonAsync(response);

            var root = json.RootElement;

            root.GetProperty("items").GetArrayLength().Should().Be(2);
            root.GetProperty("pageNumber").GetInt32().Should().Be(2);
            root.GetProperty("pageSize").GetInt32().Should().Be(2);
            root.GetProperty("totalCount").GetInt32().Should().Be(5);
            root.GetProperty("totalPages").GetInt32().Should().Be(3);
            root.GetProperty("hasNextPage").GetBoolean().Should().BeTrue();
            root.GetProperty("hasPreviousPage").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task GetSupportQueue_WhenMultipleFiltersAreProvided_AppliesAllFilters()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var otherAgent = await CreateUserAsync(
                "Other Agent",
                RoleNames.SupportAgent);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            await CreateTicketAsync(
                customer.Id,
                "Matching ticket",
                TicketPriority.High,
                agent.Id);

            await CreateTicketAsync(
                customer.Id,
                "Wrong priority",
                TicketPriority.Low,
                agent.Id);

            await CreateTicketAsync(
                customer.Id,
                "Wrong assignee",
                TicketPriority.High,
                otherAgent.Id);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await GetSupportQueueAsync(
                $"?Priority={(int)TicketPriority.High}&AssigneeId={agent.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var json = await ReadJsonAsync(response);

            var items = json.RootElement.GetProperty("items");

            items.GetArrayLength().Should().Be(1);

            items[0].GetProperty("title").GetString().Should().Be("Matching ticket");
        }
    }
}
