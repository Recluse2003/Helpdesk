using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalationDetails;
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
    public class GetEscalationDetailsEndpointTests : IntegrationTestBase
    {
        public GetEscalationDetailsEndpointTests(CustomWebApplicationFactory factory) : base(factory)
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

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var creator = await context.Users.FirstAsync(x => x.Id == agentId);

            var ticket = new Ticket(
                "Test ticket",
                "A test ticket description.",
                creator.Id);

            ticket.Claim(agentId, creator.DisplayName);

            context.Tickets.Add(ticket);

            await context.SaveChangesAsync();

            return ticket;
        }

        private async Task<EscalationRequest> CreateEscalationRequestAsync(
           Guid ticketId,
           Guid requestedByUserId,
           Guid? proposedRecipientUserId = null)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var escalationRequest = new EscalationRequest(
                "Escalation Reason.",
                ticketId,
                requestedByUserId,
                proposedRecipientUserId);

            context.EscalationRequests.Add(escalationRequest);

            await context.SaveChangesAsync();

            return escalationRequest;
        }

        private void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task GetEscalationDetails_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await Client.GetAsync($"/api/escalations/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetEscalationDetails_WhenUserIsNotSupportAgentOrAdministrator_ReturnsForbidden()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync($"/api/escalations/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetEscalationDetails_WhenEscalationRequestDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Administrator);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync($"/api/escalations/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetEscalationDetails_WhenSupportAgentIsNotRequester_ReturnsForbidden()
        {
            // Arrange
            var requester = await CreateUserAsync(
                "Requesting Agent",
                RoleNames.SupportAgent);

            var otherAgent = await CreateUserAsync(
                "Other Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateAssignedTicketAsync(requester.Id);

            var escalationRequest = await CreateEscalationRequestAsync(
                ticket.Id,
                requester.Id);

            var token = await GenerateAccessTokenAsync(otherAgent);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync($"/api/escalations/{escalationRequest.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetEscalationDetails_WhenRequesterRequestsOwnEscalation_ReturnsDetails()
        {
            // Arrange
            var requester = await CreateUserAsync(
                "Requesting Agent",
                RoleNames.SupportAgent);

            var recipient = await CreateUserAsync(
                "Proposed Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateAssignedTicketAsync(requester.Id);

            var escalationRequest = await CreateEscalationRequestAsync(
                ticket.Id,
                requester.Id,
                recipient.Id);

            var token = await GenerateAccessTokenAsync(requester);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync($"/api/escalations/{escalationRequest.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseBody = await response.Content.ReadFromJsonAsync<EscalationRequestDetailsDto>();

            responseBody.Should().NotBeNull();

            responseBody!.Id.Should().Be(escalationRequest.Id);
            responseBody.TicketId.Should().Be(ticket.Id);
            responseBody.TicketTitle.Should().Be(ticket.Title);

            responseBody.RequestedByUserId.Should().Be(requester.Id);
            responseBody.RequestedByDisplayName.Should().Be(requester.DisplayName);

            responseBody.ProposedRecipientUserId.Should().Be(recipient.Id);
            responseBody.ProposedRecipientDisplayName.Should().Be(recipient.DisplayName);

            responseBody.Reason.Should().Be(escalationRequest.Reason);
            responseBody.Status.Should().Be(EscalationRequestStatus.Waiting);

            responseBody.RespondedByUserId.Should().BeNull();
            responseBody.RespondedByDisplayName.Should().BeNull();
            responseBody.NewSupportAgentId.Should().BeNull();
            responseBody.NewSupportAgentDisplayName.Should().BeNull();
            responseBody.ResponseReason.Should().BeNull();
            responseBody.RespondedAt.Should().BeNull();
        }

        [Fact]
        public async Task GetEscalationDetails_WhenAdministratorRequestsAnotherUsersEscalation_ReturnsDetails()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var requester = await CreateUserAsync(
                "Requesting Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateAssignedTicketAsync(requester.Id);

            var escalationRequest = await CreateEscalationRequestAsync(
                ticket.Id,
                requester.Id);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync($"/api/escalations/{escalationRequest.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseBody = await response.Content.ReadFromJsonAsync<EscalationRequestDetailsDto>();

            responseBody.Should().NotBeNull();

            responseBody!.Id.Should().Be(escalationRequest.Id);
            responseBody.TicketId.Should().Be(ticket.Id);
            responseBody.RequestedByUserId.Should().Be(requester.Id);
            responseBody.RequestedByDisplayName.Should().Be(requester.DisplayName);
            responseBody.Status.Should().Be(EscalationRequestStatus.Waiting);
        }
    }
}
