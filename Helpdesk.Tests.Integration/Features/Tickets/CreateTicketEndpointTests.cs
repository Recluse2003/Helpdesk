using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Tickets.CreateTicket;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Helpdesk.Tests.Integration.Features.Tickets
{
    public class CreateTicketEndpointTests : IntegrationTestBase
    {
        public CreateTicketEndpointTests(CustomWebApplicationFactory factory) : base(factory)
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

        private void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task CreateTicket_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Authorization = null;

            var request = new CreateTicketRequest("Ticket title", "Ticket description.", TicketPriority.Medium);

            // Act
            var response = await Client.PostAsJsonAsync("api/tickets/", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateTicket_WhenUserIsNotCustomer_ReturnsForbidden()
        {
            // Arrange
            var supportAgent = await CreateUserAsync(
                "Test Support Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(supportAgent);
            Authenticate(token);

            var request = new CreateTicketRequest("Ticket title", "Ticket description.", TicketPriority.Medium);

            // Act
            var response = await Client.PostAsJsonAsync("api/tickets/", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateTicket_WhenUserIsCustomer_ReturnsTicketIDToNewTicket()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            var request = new CreateTicketRequest("Ticket title", "Ticket description.", TicketPriority.Medium);

            // Act
            var response = await Client.PostAsJsonAsync("api/tickets/", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content.ReadFromJsonAsync<CreateTicketResponse>();

            responseBody.Should().NotBeNull();

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var savedTicket = await context.Tickets.SingleAsync(x => x.Id == responseBody!.TicketId);

            savedTicket.Id.Should().Be(responseBody!.TicketId);
            savedTicket.Title.Should().Be("Ticket title");
            savedTicket.Description.Should().Be("Ticket description.");
            savedTicket.Priority.Should().Be(TicketPriority.Medium);
        }
    }
}
