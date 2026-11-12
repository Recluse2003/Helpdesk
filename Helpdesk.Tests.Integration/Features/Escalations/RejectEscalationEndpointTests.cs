using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.AcceptEscalation;
using Helpdesk.Api.Features.Escalations.RejectEscalation;
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
    public class RejectEscalationEndpointTests : IntegrationTestBase
    {
        public RejectEscalationEndpointTests(CustomWebApplicationFactory factory)
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

        private async Task<EscalationRequest> CreateActiveEscalationRequest(
            Guid ticketId,
            Guid requestedByUserId,
            Guid? proposedRecipientUserId = null)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var ticket = await context.Tickets.FirstAsync(x => x.Id == ticketId);

            var requestedByUser = await context.Users.FirstAsync(x => x.Id == requestedByUserId);

            var proposedRecipientUser = await context.Users.FirstOrDefaultAsync(x => x.Id == proposedRecipientUserId);

            var escalationRequest = new EscalationRequest(
                "Escalation reason.",
                ticket.Id,
                requestedByUser.Id,
                proposedRecipientUser == null ? null : proposedRecipientUser.Id);

            context.EscalationRequests.Add(escalationRequest);
            await context.SaveChangesAsync();

            return escalationRequest;
        }

        private void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task RejectEscalation_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Authorization = null;

            var request = new RejectEscalationRequest(
                "Response Reason.");

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/escalations/{Guid.NewGuid()}/reject",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RejectEscalation_WhenUserIsNotAnAdministrator_ReturnsForbidden()
        {
            // Arrange
            var customer = await CreateUserAsync(
                "Test Customer",
                RoleNames.Customer);

            var token = await GenerateAccessTokenAsync(customer);
            Authenticate(token);

            var request = new RejectEscalationRequest(
                "Response Reason.");

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/escalations/{Guid.NewGuid()}/reject",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RejectEscalation_WhenEscalationRequestDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.Administrator);

            var token = await GenerateAccessTokenAsync(agent);

            Authenticate(token);

            var request = new RejectEscalationRequest(
                "Response Reason.");

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/escalations/{Guid.NewGuid()}/reject",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RejectEscalation_WhenRequestHasAlreadyBeenRejected_ReturnsConflict()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var requester = await CreateUserAsync(
                "Requesting Agent",
                RoleNames.SupportAgent);

            var newAgent = await CreateUserAsync(
                "New Support Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            var ticket = await CreateAssignedTicketAsync(requester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(
                ticket.Id,
                requester.Id);

            using (var scope = Factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

                var storedRequest = await context.EscalationRequests
                    .SingleAsync(x => x.Id == escalationRequest.Id);

                storedRequest.Reject(
                    administrator.Id,
                    "Rejected.");

                await context.SaveChangesAsync();
            }

            var request = new RejectEscalationRequest(
                "Attempt to reject it again.");

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/escalations/{escalationRequest.Id}/reject",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task AcceptEscalation_WhenRequestIsValid_ReturnsNoContentAndUpdatesTicket()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var requester = await CreateUserAsync(
                "Requesting Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            var ticket = await CreateAssignedTicketAsync(requester.Id);

            var escalationRequest = await CreateActiveEscalationRequest(
                ticket.Id,
                requester.Id);

            var responseReason = "This ticket requires specialist knowledge.";

            var request = new RejectEscalationRequest(
                responseReason);

            // Act
            var response = await Client.PostAsJsonAsync(
                $"/api/escalations/{escalationRequest.Id}/reject",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.Content.Headers.ContentLength.Should().Be(0);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var savedRequest = await context.EscalationRequests
                .AsNoTracking()
                .SingleAsync(x => x.Id == escalationRequest.Id);

            savedRequest.Status.Should().Be(EscalationRequestStatus.Rejected);
            savedRequest.RespondedByUserId.Should().Be(administrator.Id);
            savedRequest.ResponseReason.Should().Be(responseReason);
            savedRequest.RespondedAt.Should().NotBeNull();

            var savedTicket = await context.Tickets
                .AsNoTracking()
                .SingleAsync(x => x.Id == ticket.Id);

            savedTicket.AssignedToId.Should().Be(requester.Id);
        }
    }
}
