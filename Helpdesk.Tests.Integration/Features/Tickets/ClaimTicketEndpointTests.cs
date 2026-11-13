using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

namespace Helpdesk.Tests.Integration.Features.Tickets
{
    public class ClaimTicketEndpointTests : IntegrationTestBase
    {
        public ClaimTicketEndpointTests(CustomWebApplicationFactory factory)
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

        private async Task<Ticket> CreateTicketAsync(Guid createdByUserId)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var ticket = new Ticket(
                "Test ticket",
                "A test ticket description.",
                createdByUserId);

            context.Tickets.Add(ticket);

            await context.SaveChangesAsync();

            return ticket;
        }

        private void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task RequestEscalation_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await Client.PostAsync($"/api/tickets/{Guid.NewGuid()}/claim", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RequestEscalation_WhenUserIsNotSupportAgent_ReturnsForbidden()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await Client.PostAsync($"/api/tickets/{Guid.NewGuid()}/claim", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RequestEscalation_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var supportAgent = await CreateUserAsync(
                "Test Support Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(supportAgent);
            Authenticate(token);

            // Act
            var response = await Client.PostAsync($"/api/tickets/{Guid.NewGuid()}/claim", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RequestEscalation_WhenRequestIsValid_ReturnsSuccessAndTicketIsClaimed()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var supportAgent = await CreateUserAsync(
                "Test Support Agent",
                RoleNames.SupportAgent);

            var ticketToClaim = await CreateTicketAsync(customer.Id);
            var ticketNotToClaim = await CreateTicketAsync(customer.Id);

            var token = await GenerateAccessTokenAsync(supportAgent);
            Authenticate(token);

            // Act
            var response = await Client.PostAsync(
                $"/api/tickets/{ticketToClaim.Id}/claim",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var claimedTicket = await context.Tickets
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == ticketToClaim.Id);

            claimedTicket.Should().NotBeNull();
            claimedTicket!.AssignedToId.Should().Be(supportAgent.Id);

            var notClaimedTicket = await context.Tickets
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == ticketNotToClaim.Id);

            notClaimedTicket.Should().NotBeNull();
            notClaimedTicket!.AssignedToId.Should().BeNull();
        }
    }
}
