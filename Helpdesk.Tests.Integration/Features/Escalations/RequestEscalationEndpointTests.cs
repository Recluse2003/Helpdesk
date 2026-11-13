using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.RequestEscalation;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Helpdesk.Tests.Integration.Features.Escalations
{
    public class RequestEscalationEndpointTests : IntegrationTestBase
    {
        public RequestEscalationEndpointTests(CustomWebApplicationFactory factory)
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

        private async Task<Ticket> CreateAssignedTicketAsync(Guid agentId)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            // A real user is used as the ticket creator.
            var creator = await context.Users
                .FirstAsync(x => x.Id == agentId);

            var ticket = new Ticket(
                "Test ticket",
                "A test ticket description.",
                creator.Id);

            ticket.Claim(agentId, creator.DisplayName);

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

            var request = new RequestEscalationRequest(
                "Escalation required.",
                Guid.NewGuid(),
                null);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/escalations",
                request);

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

            var request = new RequestEscalationRequest(
                "Escalation required.",
                Guid.NewGuid(),
                null);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/escalations",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RequestEscalation_WhenTicketDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            var request = new RequestEscalationRequest(
                "Escalation required.",
                Guid.NewGuid(),
                null);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/escalations",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RequestEscalation_WhenAgentIsNotAssignedToTicket_ReturnsForbidden()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var otherAgent = await CreateUserAsync(
                "Other Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);

            Authenticate(token);

            var ticket = await CreateAssignedTicketAsync(otherAgent.Id);

            var request = new RequestEscalationRequest(
                "Escalation required.",
                ticket.Id,
                null);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/escalations",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RequestEscalation_WhenProposedRecipientIsNotFound_ReturnsNotFound()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            var ticket = await CreateAssignedTicketAsync(agent.Id);

            var request = new RequestEscalationRequest(
                "Escalation required.",
                ticket.Id,
                Guid.NewGuid());

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/escalations",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RequestEscalation_WhenProposedRecipientIsNotSupportAgent_ReturnsBadRequest()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            var ticket = await CreateAssignedTicketAsync(agent.Id);

            var request = new RequestEscalationRequest(
                "Escalation required.",
                ticket.Id,
                customer.Id);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/escalations",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RequestEscalation_WhenProposedRecipientIsRequester_ReturnsBadRequest()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            var ticket = await CreateAssignedTicketAsync(agent.Id);

            var request = new RequestEscalationRequest(
                "Escalation required.",
                ticket.Id,
                agent.Id);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/escalations",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RequestEscalation_WhenRequestIsValidWithoutProposedRecipient_ReturnsCreated()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            var ticket = await CreateAssignedTicketAsync(agent.Id);

            var request = new RequestEscalationRequest(
                "The ticket requires specialist support.",
                ticket.Id,
                null);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/escalations",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content
                .ReadFromJsonAsync<RequestEscalationResponse>();

            responseBody.Should().NotBeNull();
            responseBody!.EscalationRequestId.Should().NotBeEmpty();

            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should().Be($"/api/escalations/{responseBody.EscalationRequestId}");

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var savedRequest = await context.EscalationRequests
                .SingleOrDefaultAsync(x => x.Id == responseBody.EscalationRequestId);

            savedRequest.Should().NotBeNull();
            savedRequest!.TicketId.Should().Be(ticket.Id);
            savedRequest.RequestedByUserId.Should().Be(agent.Id);
            savedRequest.ProposedRecipientUserId.Should().BeNull();
            savedRequest.Reason.Should().Be(request.Reason);
            savedRequest.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public async Task RequestEscalation_WhenRequestIsValidWithProposedRecipient_ReturnsCreatedAndPersistsRecipient()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var recipient = await CreateUserAsync(
                "Proposed Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);
            
            Authenticate(token);

            var ticket = await CreateAssignedTicketAsync(agent.Id);

            var request = new RequestEscalationRequest(
                "This ticket needs a specialist.",
                ticket.Id,
                recipient.Id);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/escalations",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content
                .ReadFromJsonAsync<RequestEscalationResponse>();

            responseBody.Should().NotBeNull();

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var savedRequest = await context.EscalationRequests
                .SingleAsync(x => x.Id == responseBody!.EscalationRequestId);

            savedRequest.ProposedRecipientUserId.Should().Be(recipient.Id);
            savedRequest.RequestedByUserId.Should().Be(agent.Id);
            savedRequest.TicketId.Should().Be(ticket.Id);
        }
    }
}
