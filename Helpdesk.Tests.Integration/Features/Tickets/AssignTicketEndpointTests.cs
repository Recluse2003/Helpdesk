using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

namespace Helpdesk.Tests.Integration.Features.Tickets.AssignTicket
{
    public class AssignTicketEndpointTests : IntegrationTestBase
    {
        public AssignTicketEndpointTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        private async Task<ApplicationUser> CreateUserAsync(string displayName, string role)
        {
            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
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

        private async Task<Ticket> CreateTicketAsync(Guid creatorId, Guid? assignedAgentId = null)
        {
            var ticket = new Ticket(
                "Test ticket",
                "Test ticket description.",
                creatorId,
                TicketPriority.Medium);

            if (assignedAgentId.HasValue)
            {
                ticket.Claim(assignedAgentId.Value, "Previous Agent");
            }

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

        private async Task<HttpResponseMessage> AssignTicketAsync(Guid ticketId, Guid assigneeId)
        {
            return await Client.PostAsJsonAsync(
                $"/api/tickets/{ticketId}/assign",
                new { AssigneeId = assigneeId });
        }

        [Fact]
        public async Task AssignTicket_WhenUnauthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var ticketId = Guid.NewGuid();

            // Act
            var response = await AssignTicketAsync(ticketId, Guid.NewGuid());

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData(RoleNames.Customer)]
        [InlineData(RoleNames.SupportAgent)]
        public async Task AssignTicket_WhenUserIsNotAdministrator_ReturnsForbidden(string role)
        {
            // Arrange
            var user = await CreateUserAsync("Test User", role);
            var token = await GenerateAccessTokenAsync(user);

            Authenticate(token);

            // Act
            var response = await AssignTicketAsync(
                Guid.NewGuid(),
                Guid.NewGuid());

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AssignTicket_WhenAssigneeDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Administrator",
                RoleNames.Administrator);

            var ticket = await CreateTicketAsync(administrator.Id);

            var token = await GenerateAccessTokenAsync(administrator);

            Authenticate(token);

            // Act
            var response = await AssignTicketAsync(
                ticket.Id,
                Guid.NewGuid());

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AssignTicket_WhenAssigneeIsNotSupportAgent_ReturnsBadRequest()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Administrator",
                RoleNames.Administrator);

            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var ticket = await CreateTicketAsync(administrator.Id);

            var token = await GenerateAccessTokenAsync(administrator);

            Authenticate(token);

            // Act
            var response = await AssignTicketAsync(
                ticket.Id,
                customer.Id);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AssignTicket_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Administrator",
                RoleNames.Administrator);

            var assignee = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(administrator);

            Authenticate(token);

            // Act
            var response = await AssignTicketAsync(
                Guid.NewGuid(),
                assignee.Id);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AssignTicket_WhenRequestIsValid_AssignsTicketAndReturnsNoContent()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Administrator",
                RoleNames.Administrator);

            var assignee = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(administrator.Id);

            var token = await GenerateAccessTokenAsync(administrator);

            Authenticate(token);

            // Act
            var response = await AssignTicketAsync(
                ticket.Id,
                assignee.Id);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var updatedTicket = await context.Tickets
                .AsNoTracking()
                .FirstAsync(x => x.Id == ticket.Id);

            updatedTicket.AssignedToId.Should().Be(assignee.Id);
        }

        [Fact]
        public async Task AssignTicket_WhenTicketAlreadyAssigned_ReassignsTicketAndReturnsNoContent()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Administrator",
                RoleNames.Administrator);

            var previousAgent = await CreateUserAsync(
                "Previous Agent",
                RoleNames.SupportAgent);

            var newAgent = await CreateUserAsync(
                "New Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(
                administrator.Id,
                previousAgent.Id);

            var token = await GenerateAccessTokenAsync(administrator);

            Authenticate(token);

            // Act
            var response = await AssignTicketAsync(
                ticket.Id,
                newAgent.Id);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var updatedTicket = await context.Tickets
                .AsNoTracking()
                .FirstAsync(x => x.Id == ticket.Id);

            updatedTicket.AssignedToId.Should().Be(newAgent.Id);
        }
    }
}