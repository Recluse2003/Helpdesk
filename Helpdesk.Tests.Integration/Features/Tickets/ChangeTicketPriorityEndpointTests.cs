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
using System.Net.Http.Json;

namespace Helpdesk.Tests.Integration.Features.Tickets.ChangeTicketPriority
{
    public class ChangeTicketPriorityEndpointTests : IntegrationTestBase
    {
        public ChangeTicketPriorityEndpointTests(CustomWebApplicationFactory factory)
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

        private async Task<Ticket> CreateTicketAsync(Guid creatorId, Guid? assignedAgentId = null, bool closed = false)
        {
            var ticket = new Ticket(
                "Test ticket",
                "Test ticket description.",
                creatorId,
                TicketPriority.Low);

            if (assignedAgentId.HasValue)
                ticket.Claim(assignedAgentId.Value, "Test Agent");

            if (closed)
            {
                ticket.Resolve();
                ticket.Close();
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

        private async Task<HttpResponseMessage> ChangePriorityAsync(
            Guid ticketId,
            TicketPriority priority)
        {
            return await Client.PatchAsJsonAsync(
                $"/api/tickets/{ticketId}/priority",
                new { Priority = priority });
        }

        [Fact]
        public async Task ChangePriority_WhenUnauthenticated_ReturnsUnauthorized()
        {
            // Act
            var response = await ChangePriorityAsync(
                Guid.NewGuid(),
                TicketPriority.High);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangePriority_WhenUserIsCustomer_ReturnsForbidden()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await ChangePriorityAsync(
                Guid.NewGuid(),
                TicketPriority.High);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ChangePriority_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await ChangePriorityAsync(
                Guid.NewGuid(),
                TicketPriority.High);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ChangePriority_WhenAgentIsNotAssignedToTicket_ReturnsForbidden()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var otherAgent = await CreateUserAsync(
                "Other Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(
                agent.Id,
                otherAgent.Id);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await ChangePriorityAsync(
                ticket.Id,
                TicketPriority.High);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var unchangedTicket = await context.Tickets
                .AsNoTracking()
                .FirstAsync(x => x.Id == ticket.Id);

            unchangedTicket.Priority.Should().Be(TicketPriority.Low);
        }

        [Fact]
        public async Task ChangePriority_WhenTicketIsClosed_ReturnsConflict()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(
                agent.Id,
                agent.Id,
                closed: true);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await ChangePriorityAsync(
                ticket.Id,
                TicketPriority.High);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var unchangedTicket = await context.Tickets
                .AsNoTracking()
                .FirstAsync(x => x.Id == ticket.Id);

            unchangedTicket.Priority.Should().Be(TicketPriority.Low);
        }

        [Fact]
        public async Task ChangePriority_WhenAssignedAgentChangesPriority_ReturnsNoContent()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(
                agent.Id,
                agent.Id);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await ChangePriorityAsync(
                ticket.Id,
                TicketPriority.High);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var updatedTicket = await context.Tickets
                .AsNoTracking()
                .FirstAsync(x => x.Id == ticket.Id);

            updatedTicket.Priority.Should().Be(TicketPriority.High);
        }

        [Fact]
        public async Task ChangePriority_WhenAdministratorChangesUnassignedTicket_ReturnsNoContent()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Administrator",
                RoleNames.Administrator);

            var ticket = await CreateTicketAsync(administrator.Id);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            // Act
            var response = await ChangePriorityAsync(
                ticket.Id,
                TicketPriority.Urgent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var updatedTicket = await context.Tickets
                .AsNoTracking()
                .FirstAsync(x => x.Id == ticket.Id);

            updatedTicket.Priority.Should().Be(TicketPriority.Urgent);
        }

        [Fact]
        public async Task ChangePriority_WhenAdministratorChangesAnotherAgentsTicket_ReturnsNoContent()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Administrator",
                RoleNames.Administrator);

            var agent = await CreateUserAsync(
                "Support Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(
                administrator.Id,
                agent.Id);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            // Act
            var response = await ChangePriorityAsync(
                ticket.Id,
                TicketPriority.High);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var updatedTicket = await context.Tickets
                .AsNoTracking()
                .FirstAsync(x => x.Id == ticket.Id);

            updatedTicket.Priority.Should().Be(TicketPriority.High);
        }
    }
}