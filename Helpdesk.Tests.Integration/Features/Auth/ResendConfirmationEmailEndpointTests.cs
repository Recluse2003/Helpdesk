using FluentAssertions;
using Helpdesk.Api.Features.Auth.ResendEmailConfirmation;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Helpdesk.Tests.Integration.Features.Auth
{
    public class ResendConfirmationEmailEndpointTests : IntegrationTestBase
    {
        public ResendConfirmationEmailEndpointTests(CustomWebApplicationFactory factory) : base(factory)
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
                EmailConfirmed = false
            };

            var result = await userManager.CreateAsync(user, "Password123!");

            var roleResult = await userManager.AddToRoleAsync(user, role);

            return user;
        }

        [Fact]
        public async Task ResendConfirmationEmail_WhenUserDoesNotExist_ReturnsNoContent()
        {
            // Arrange
            var request = new ResendConfirmationEmailCommand("missing@example.com");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/resend-confirmation-email",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task ResendConfirmationEmail_WhenEmailAlreadyConfirmed_ReturnsNoContent()
        {
            // Arrange
            var user = await CreateUserAsync("Test Customer", "Customer");

            var request = new ResendConfirmationEmailCommand(user.Email!);

            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var trackedUser = await userManager.FindByIdAsync(user.Id.ToString());

            trackedUser!.EmailConfirmed = true;

            var updateResult = await userManager.UpdateAsync(trackedUser);
            updateResult.Succeeded.Should().BeTrue();

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/resend-confirmation-email", 
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task ResendConfirmationEmail_WhenUserIsUnconfirmed_ReturnsNoContent()
        {
            // Arrange
            var user = await CreateUserAsync("Test Customer", "Customer");

            var request = new ResendConfirmationEmailCommand(user.Email!);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/resend-confirmation-email", 
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
