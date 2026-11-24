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
using System.Text.Json;
using System.Text.Json.Serialization;

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

        private async Task<Ticket> CreateTicketAsync(Guid creatorId, Guid? agentId = null, string? ticketTitle = null)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var ticket = new Ticket(
                ticketTitle != null ? ticketTitle : "Ticket title",
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

            var result = await response.Content.ReadFromJsonAsync<GetTicketsByUserIdResponse>();

            result.Should().NotBeNull();
            result!.Tickets.Items.Should().BeEmpty();
            result.Tickets.TotalCount.Should().Be(0);
            result.Statistics.TotalTickets.Should().Be(0);
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

            var result = await response.Content.ReadFromJsonAsync<GetTicketsByUserIdResponse>(
                TestJsonOptions.Default
            );

            result!.Tickets.Items.Should().NotBeNull();
            result!.Tickets.Items.Should().ContainSingle();
            result.Tickets.Items.Should().Contain(x => x.Id == ownTicket.Id);
            result.Tickets.Items.Should().NotContain(x => x.Id == anotherTicket.Id);

            result.Statistics.TotalTickets.Should().Be(1);
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

            var result = await response.Content.ReadFromJsonAsync<GetTicketsByUserIdResponse>(
                TestJsonOptions.Default
            );

            result.Tickets.Should().NotBeNull();

            result!.Tickets.Items.Should().ContainSingle(x =>
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

            var result = await response.Content.ReadFromJsonAsync<GetTicketsByUserIdResponse>(
                TestJsonOptions.Default
            );

            result!.Tickets.Should().NotBeNull();

            result!.Tickets.Items.Should().ContainSingle(x =>
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

            var result = await response.Content.ReadFromJsonAsync<GetTicketsByUserIdResponse>(
                TestJsonOptions.Default
            );

            result.Tickets.Should().NotBeNull();
            result!.Tickets.Items.Should().HaveCount(2);
            result.Tickets.TotalCount.Should().Be(5);
            result.Tickets.PageNumber.Should().Be(1);
            result.Tickets.PageSize.Should().Be(2);
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

            var result = await response.Content.ReadFromJsonAsync<GetTicketsByUserIdResponse>(
                TestJsonOptions.Default
            );

            result.Tickets.Should().NotBeNull();
            result!.Tickets.Items.Should().HaveCount(2);
            result.Tickets.PageNumber.Should().Be(2);
            result.Tickets.TotalCount.Should().Be(5);
        }

        [Fact]
        public async Task GetMyTickets_WhenSearchTermIsSpecified_ReturnsOnlySpecifiedTickets()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            await CreateTicketAsync(customer.Id, null, "target");
            await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=1&pageSize=10&searchTerm=target");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<GetTicketsByUserIdResponse>(
                TestJsonOptions.Default
            );

            result.Tickets.Should().NotBeNull();
            result!.Tickets.Items.Should().HaveCount(1);

            result!.Tickets.Items.Should().ContainSingle(x => x.Title == "target");
        }


        [Fact]
        public async Task GetMyTickets_WhenSearchTermIsSpecified_ReturnsOnlySpecifiedTickets()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            await CreateTicketAsync(customer.Id, null, "target");
            await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await GetTicketsByUserIdAsync("?pageNumber=1&pageSize=10&searchTerm=target");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<GetTicketsByUserIdResponse>(
                TestJsonOptions.Default
            );

            result.Tickets.Should().NotBeNull();
            result!.Tickets.Items.Should().HaveCount(1);

            result!.Tickets.Items.Should().ContainSingle(x => x.Title == "target");
        }
    }
}
