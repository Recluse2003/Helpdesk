
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Domain.Enums;
using Helpdesk.Api.Features.Escalations.GetEscalations;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Helpdesk.Tests.Integration.Features.Escalations
{
    public class GetEscalationsEndpointTests : IntegrationTestBase
    {
        public GetEscalationsEndpointTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        private async Task<ApplicationUser> CreateUserAsync(
            string displayName,
            string role)
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

        private async Task<Ticket> CreateTicketAsync(Guid creatorId, string title = "Test ticket")
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var ticket = new Ticket(
                title,
                "A test ticket description.",
                creatorId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            return ticket;
        }

        private async Task<EscalationRequest> CreateEscalationRequestAsync(
            Guid ticketId,
            Guid requesterId,
            Guid? proposedRecipientId = null,
            EscalationRequestStatus status = EscalationRequestStatus.Waiting,
            Guid? responderId = null,
            Guid? newSupportAgentId = null)
        {
            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var request = new EscalationRequest(
                "The ticket requires specialist support.",
                ticketId,
                requesterId,
                proposedRecipientId);

            if (status == EscalationRequestStatus.Accepted)
            {
                if (!responderId.HasValue || !newSupportAgentId.HasValue)
                    throw new ArgumentException( "An accepted request requires a responder and a new support agent.");

                request.Accept(
                    responderId.Value,
                    newSupportAgentId.Value,
                    "Escalation accepted.");
            }
            else if (status == EscalationRequestStatus.Rejected)
            {
                if (!responderId.HasValue)
                    throw new ArgumentException("A rejected request requires a responder.");

                request.Reject(
                    responderId.Value,
                    "Escalation rejected.");
            }

            context.EscalationRequests.Add(request);
            await context.SaveChangesAsync();

            return request;
        }

        private void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task GetEscalations_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await Client.GetAsync("/api/escalations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetEscalations_WhenUserIsNotAdministrator_ReturnsForbidden()
        {
            // Arrange
            var agent = await CreateUserAsync(
                "Test Agent",
                RoleNames.SupportAgent);

            var token = await GenerateAccessTokenAsync(agent);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync("/api/escalations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetEscalations_WhenNoRequestsExist_ReturnsEmptyPagedList()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync("/api/escalations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<EscalationRequestDto>>();

            result.Should().NotBeNull();
            result!.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalPages.Should().Be(0);
            result.HasNextPage.Should().BeFalse();
            result.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetEscalations_WhenRequestsExist_ReturnsTheirDetails()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var requester = await CreateUserAsync(
                "Requesting Agent",
                RoleNames.SupportAgent);

            var recipient = await CreateUserAsync(
                "Proposed Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(requester.Id);

            var escalationRequest = await CreateEscalationRequestAsync(
                ticket.Id,
                requester.Id,
                recipient.Id);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync("/api/escalations");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<EscalationRequestDto>>();

            result.Should().NotBeNull();
            result!.Items.Should().ContainSingle();

            var dto = result.Items.Single();

            dto.Id.Should().Be(escalationRequest.Id);
            dto.TicketId.Should().Be(ticket.Id);
            dto.TicketTitle.Should().Be(ticket.Title);
            dto.RequestedByUserId.Should().Be(requester.Id);
            dto.RequestedByDisplayName.Should().Be(requester.DisplayName);
            dto.ProposedRecipientUserId.Should().Be(recipient.Id);
            dto.ProposedRecipientDisplayName.Should().Be(recipient.DisplayName);
            dto.Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public async Task GetEscalations_WhenStatusFilterIsProvided_ReturnsOnlyMatchingRequests()
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

            var ticket = await CreateTicketAsync(requester.Id);

            var waitingRequest = await CreateEscalationRequestAsync(
                ticket.Id,
                requester.Id);

            await CreateEscalationRequestAsync(
                ticket.Id,
                requester.Id,
                status: EscalationRequestStatus.Accepted,
                responderId: administrator.Id,
                newSupportAgentId: newAgent.Id);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            var url = $"/api/escalations?status={EscalationRequestStatus.Waiting}";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<EscalationRequestDto>>();

            result.Should().NotBeNull();
            result!.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();

            result.Items.Single().Id.Should().Be(waitingRequest.Id);
            result.Items.Single().Status.Should().Be(EscalationRequestStatus.Waiting);
        }

        [Fact]
        public async Task GetEscalations_WhenTicketIdFilterIsProvided_ReturnsOnlyMatchingRequests()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var requester = await CreateUserAsync(
                "Requesting Agent",
                RoleNames.SupportAgent);

            var firstTicket = await CreateTicketAsync(requester.Id, "First ticket");
            var secondTicket = await CreateTicketAsync(requester.Id, "Second ticket");

            var firstRequest = await CreateEscalationRequestAsync(
                firstTicket.Id,
                requester.Id);

            await CreateEscalationRequestAsync(
                secondTicket.Id,
                requester.Id);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            var url = $"/api/escalations?ticketId={firstTicket.Id}";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<EscalationRequestDto>>();

            result.Should().NotBeNull();
            result!.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();

            result.Items.Single().Id.Should().Be(firstRequest.Id);
            result.Items.Single().TicketId.Should().Be(firstTicket.Id);
        }

        [Fact]
        public async Task GetEscalations_WhenRequestedByUserIdFilterIsProvided_ReturnsOnlyMatchingRequests()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var firstRequester = await CreateUserAsync(
                "First Agent",
                RoleNames.SupportAgent);

            var secondRequester = await CreateUserAsync(
                "Second Agent",
                RoleNames.SupportAgent);

            var firstTicket = await CreateTicketAsync(firstRequester.Id);
            var secondTicket = await CreateTicketAsync(secondRequester.Id);

            var firstRequest = await CreateEscalationRequestAsync(
                firstTicket.Id,
                firstRequester.Id);

            await CreateEscalationRequestAsync(
                secondTicket.Id,
                secondRequester.Id);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            var url = $"/api/escalations?requestedByUserId={firstRequester.Id}";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<EscalationRequestDto>>();

            result.Should().NotBeNull();
            result!.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();

            result.Items.Single().Id.Should().Be(firstRequest.Id);
            result.Items.Single().RequestedByUserId.Should().Be(firstRequester.Id);
        }

        [Fact]
        public async Task GetEscalations_WhenMultipleFiltersAreProvided_AppliesAllFilters()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var firstRequester = await CreateUserAsync(
                "First Agent",
                RoleNames.SupportAgent);

            var secondRequester = await CreateUserAsync(
                "Second Agent",
                RoleNames.SupportAgent);

            var firstTicket = await CreateTicketAsync(firstRequester.Id);
            var secondTicket = await CreateTicketAsync(secondRequester.Id);

            var matchingRequest = await CreateEscalationRequestAsync(
                firstTicket.Id,
                firstRequester.Id);

            await CreateEscalationRequestAsync(
                firstTicket.Id,
                secondRequester.Id);

            await CreateEscalationRequestAsync(
                secondTicket.Id,
                firstRequester.Id);

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            var url =
                $"/api/escalations?status={EscalationRequestStatus.Waiting}" +
                $"&ticketId={firstTicket.Id}" +
                $"&requestedByUserId={firstRequester.Id}";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<EscalationRequestDto>>();

            result.Should().NotBeNull();
            result!.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();

            result.Items.Single().Id.Should().Be(matchingRequest.Id);
        }

        [Fact]
        public async Task GetEscalations_WhenPageSizeIsProvided_ReturnsRequestedNumberOfItems()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var requester = await CreateUserAsync(
                "Requesting Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(requester.Id);

            for (var i = 0; i < 5; i++)
            {
                await CreateEscalationRequestAsync(ticket.Id, requester.Id);
            }

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync("/api/escalations?pageNumber=1&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<EscalationRequestDto>>();

            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.TotalPages.Should().Be(3);
            result.HasNextPage.Should().BeTrue();
            result.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetEscalations_WhenSecondPageIsRequested_ReturnsSecondPage()
        {
            // Arrange
            var administrator = await CreateUserAsync(
                "Test Administrator",
                RoleNames.Administrator);

            var requester = await CreateUserAsync(
                "Requesting Agent",
                RoleNames.SupportAgent);

            var ticket = await CreateTicketAsync(requester.Id);

            for (var i = 0; i < 5; i++)
            {
                await CreateEscalationRequestAsync(ticket.Id, requester.Id);
            }

            var token = await GenerateAccessTokenAsync(administrator);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync("/api/escalations?pageNumber=2&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<PagedList<EscalationRequestDto>>();

            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(2);
            result.HasNextPage.Should().BeTrue();
            result.HasPreviousPage.Should().BeTrue();
        }
    }
}
