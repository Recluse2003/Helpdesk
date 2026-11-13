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
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Helpdesk.Tests.Integration.Features.Tickets
{
    public class AddTicketMessageEndpointTests : IntegrationTestBase
    {
        public AddTicketMessageEndpointTests(CustomWebApplicationFactory factory)
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

        private async Task<Ticket> CreateAssignedTicketAsync(ApplicationUser creator, ApplicationUser assignedAgent)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var ticket = new Ticket(
                "Test ticket",
                "A test ticket description.",
                creator.Id,
                TicketPriority.Medium);

            ticket.Claim(assignedAgent.Id, assignedAgent.DisplayName);

            context.Tickets.Add(ticket);

            await context.SaveChangesAsync();

            return ticket;
        }

        private void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task<HttpResponseMessage> AddMessageAsync(Guid ticketId, string content, bool isPublic)
        {
            return await Client.PostAsJsonAsync(
                $"/api/tickets/{ticketId}/message",
                new
                {
                    Content = content,
                    IsPublic = isPublic
                });
        }

        [Fact]
        public async Task AddMessage_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await AddMessageAsync(
                Guid.NewGuid(),
                "Test message",
                true);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AddMessage_WhenCustomerAddsInternalNote_ReturnsForbidden()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateAssignedTicketAsync(customer, agent);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await AddMessageAsync(
                ticket.Id,
                "This is an internal note.",
                false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AddMessage_WhenAssignedAgentAddsInternalNote_ReturnsNoContent()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateAssignedTicketAsync(customer, agent);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await AddMessageAsync(
                ticket.Id,
                "Internal troubleshooting details.",
                false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var updatedTicket = await context.Tickets
                .AsNoTracking()
                .Include(t => t.Messages)
                .SingleAsync(t => t.Id == ticket.Id);

            updatedTicket.Messages.Should().ContainSingle();

            var message = updatedTicket.Messages.Single();

            message.IsPublic.Should().BeFalse();
        }

        [Fact]
        public async Task AddMessage_WhenAdministratorAddsInternalNote_ReturnsNoContent()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var agent = await CreateUserAsync(
                "Assigned Agent",
                RoleNames.SupportAgent);

            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var ticket = await CreateAssignedTicketAsync(customer, agent);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            // Act
            var response = await AddMessageAsync(
                ticket.Id,
                "Administrator internal note.",
                false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var updatedTicket = await context.Tickets
                .AsNoTracking()
                .Include(t => t.Messages)
                .SingleAsync(t => t.Id == ticket.Id);

            updatedTicket.Messages.Should().ContainSingle();

            var message = updatedTicket.Messages.Single();

            message.IsPublic.Should().BeFalse();
        }

        [Fact]
        public async Task AddMessage_WhenCustomerAddsPublicMessage_ReturnsNoContent()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateAssignedTicketAsync(customer, agent);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await AddMessageAsync(
                ticket.Id,
                "I have more information about my issue.",
                true);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var updatedTicket = await context.Tickets
                .AsNoTracking()
                .Include(t => t.Messages)
                .SingleAsync(t => t.Id == ticket.Id);

            updatedTicket.Messages.Should().ContainSingle();

            var message = updatedTicket.Messages.Single();

            message.IsPublic.Should().BeTrue();
        }

        [Fact]
        public async Task AddMessage_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await AddMessageAsync(
                Guid.NewGuid(),
                "Test message",
                true);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
