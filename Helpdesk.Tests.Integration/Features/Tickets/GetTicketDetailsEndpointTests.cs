using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.GetTicketDetails;
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
    public class GetTicketDetailsEndpointTests : IntegrationTestBase
    {
        public GetTicketDetailsEndpointTests(CustomWebApplicationFactory factory) : base(factory)
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

        private async Task<Ticket> CreateTicketAsync(Guid creatorId)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var ticket = new Ticket(
                "Test ticket",
                "A test ticket description.",
                creatorId,
                TicketPriority.Medium);

            context.Tickets.Add(ticket);

            await context.SaveChangesAsync();

            return ticket;
        }

        private void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task GetTicketDetails_WhenUnauthenticated_ReturnsUnauthorized()
        {
            // Act
            var response = await Client.GetAsync(
                $"/api/tickets/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetTicketDetails_WhenCustomerRequestsOwnTicket_ReturnsOk()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var ticket = await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync(
                $"/api/tickets/{ticket.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetTicketDetails_WhenCustomerRequestsAnotherCustomersTicket_ReturnsNotFound()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var anotherCustomer = await CreateUserAsync(
                "Another Customer",
                RoleNames.Customer);

            var ticket = await CreateTicketAsync(anotherCustomer.Id);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync(
                $"/api/tickets/{ticket.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTicketDetails_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync(
                $"/api/tickets/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTicketDetails_WhenSupportAgentRequestsTicket_ReturnsOk()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var ticket = await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync(
                $"/api/tickets/{ticket.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetTicketDetails_WhenAdministratorRequestsTicket_ReturnsOk()
        {
            // Arrange
            var admin = await CreateUserAsync(
                "Administrator",
                RoleNames.Administrator);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var ticket = await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(admin);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync(
                $"/api/tickets/{ticket.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetTicketDetails_WhenCustomerRequestsOwnTicket_ReturnsTicketDetails()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var ticket = await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync(
                $"/api/tickets/{ticket.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<TicketDetailsDto>();

            result.Should().NotBeNull();
            result!.Id.Should().Be(ticket.Id);
            result.Title.Should().Be(ticket.Title);
            result.Description.Should().Be(ticket.Description);
        }
    }
}
