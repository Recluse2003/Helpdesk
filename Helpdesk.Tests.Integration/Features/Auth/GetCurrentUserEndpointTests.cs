using FluentAssertions;
using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Helpdesk.Tests.Integration.Features.Auth
{
    public class GetCurrentUserEndpointTests : IntegrationTestBase
    {
        public GetCurrentUserEndpointTests(CustomWebApplicationFactory factory) : base(factory)
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
        public async Task GetCurrentUser_WhenUserDoesNotExist_ReturnsUnauthorized()
        {
            // Arrange

            // Act
            var response = await Client.GetAsync(
                "/api/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetCurrentUser_WhenUserExists_ReturnsCurrentUserDetails()
        {
            // Arrange
            var userDisplayName = "Test User";
            var userRole = RoleNames.SupportAgent;
            var user = await CreateUserAsync(userDisplayName, userRole);

            var token = await GenerateAccessTokenAsync(user);
            Authenticate(token);

            // Act
            var response = await Client.GetAsync(
                "/api/auth/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            json.RootElement.GetProperty("id").GetGuid().Should().Be(user.Id);
            json.RootElement.GetProperty("email").GetString().Should().Be(user.Email);
            json.RootElement.GetProperty("displayName").GetString().Should().Be(user.DisplayName);

            var roles = json.RootElement.GetProperty("roles");

            roles.GetArrayLength().Should().Be(1);
            roles[0].GetString().Should().Be(RoleNames.SupportAgent);

        }
    }
}
