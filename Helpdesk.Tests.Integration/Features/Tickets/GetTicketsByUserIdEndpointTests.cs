using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.GetTicketsByUserId;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Helpdesk.Tests.Integration.Features.Tickets
{
    public class GetTicketsByUserIdEndpointTests : IntegrationTestBase
    {
        public GetTicketsByUserIdEndpointTests(CustomWebApplicationFactory factory) : base(factory)
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

            var result = await userManager.CreateAsync(user, "Password123!");

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

        private async Task<Ticket> CreateTicketAsync(Guid creatorId, Guid? agentId = null)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var ticket = new Ticket(
                "Ticket title",
                "Ticket description",
                creatorId,
                TicketPriority.Medium);

            if (agentId.HasValue)
                ticket.Claim((Guid)agentId, "User Id");

            context.Tickets.Add(ticket);

            await context.SaveChangesAsync();

            return ticket;
        }

        private void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task<HttpResponseMessage> GetTicketsByUserIdAsync(string query = "")
        {
            return await Client.GetAsync($"/api/tickets/me{query}");
        }

        [Fact]
        public async Task GetMyTickets_WhenUnauthenticated_ReturnsUnauthorized()
        {
            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMyTickets_WhenAuthenticated_ReturnsOk()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetMyTickets_WhenUserHasNoTickets_ReturnsEmptyList()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<TicketSummaryDto>>();

            result.Should().NotBeNull();
            result!.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetMyTickets_WhenUserHasTickets_ReturnsTheirTicketsOnly()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var anotherCustomer = await CreateUserAsync(
                "Another Customer",
                RoleNames.Customer);

            var ownTicket = await CreateTicketAsync(customer.Id);
            var anotherTicket = await CreateTicketAsync(anotherCustomer.Id);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<TicketSummaryDto>>();

            result.Should().NotBeNull();
            result!.Items.Should().ContainSingle();
            result.Items.Should().Contain(x => x.Id == ownTicket.Id);
            result.Items.Should().NotContain(x => x.Id == anotherTicket.Id);
        }

        [Fact]
        public async Task GetMyTickets_WhenTicketIsUnassigned_ReturnsNullAssignedAgentName()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var ticket = await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<TicketSummaryDto>>();

            result.Should().NotBeNull();

            result!.Items.Should().ContainSingle(x =>
                x.Id == ticket.Id &&
                x.AssignedAgentUsername == null);
        }

        [Fact]
        public async Task GetMyTickets_WhenTicketIsAssigned_ReturnsAssignedAgentDisplayName()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(customer.Id, agent.Id);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<TicketSummaryDto>>();

            result.Should().NotBeNull();

            result!.Items.Should().ContainSingle(x =>
                x.Id == ticket.Id &&
                x.AssignedAgentUsername == agent.DisplayName);
        }

        [Fact]
        public async Task GetMyTickets_WhenPageSizeIsSpecified_ReturnsRequestedNumberOfTickets()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            for (int i = 0; i < 5; i++)
            {
                await CreateTicketAsync(customer.Id);
            }

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=1&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<TicketSummaryDto>>();

            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
        }

        [Fact]
        public async Task GetMyTickets_WhenRequestingSecondPage_ReturnsSecondPage()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            for (int i = 0; i < 5; i++)
            {
                await CreateTicketAsync(customer.Id);
            }

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=2&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<TicketSummaryDto>>();

            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(2);
            result.PageNumber.Should().Be(2);
            result.TotalCount.Should().Be(5);
        }
    }
}
