using FluentAssertions;
using Helpdesk.Api.Features.Auth.ForgotPassword;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Helpdesk.Tests.Integration.Features.Auth
{
    public class ForgotPasswordEndpointTests : IntegrationTestBase
    {
        public ForgotPasswordEndpointTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<ApplicationUser> CreateUserAsync(string displayName, string role, bool emailConfirmed = false)
        {
            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = $"user-{Guid.NewGuid()}@example.com",
                Email = $"user-{Guid.NewGuid()}@example.com",
                DisplayName = displayName,
                EmailConfirmed = emailConfirmed
            };

            var result = await userManager.CreateAsync(user, "Password123!");

            var roleResult = await userManager.AddToRoleAsync(user, role);

            return user;
        }

        [Fact]
        public async Task ForgotPassword_WhenUserDoesNotExist_ReturnsNoContent()
        {
            // Arrange
            var request = new ForgotPasswordCommand("missing@example.com");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task ForgotPassword_WhenEmailIsNotConfirmed_ReturnsNoContent()
        {
            // Arrange
            var user = await CreateUserAsync("Test Customer", "Customer");

            var request = new ForgotPasswordCommand(user.Email!);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the account remains unconfirmed.
            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var persistedUser = await userManager.FindByIdAsync(user.Id.ToString());

            persistedUser.Should().NotBeNull();
            persistedUser!.EmailConfirmed.Should().BeFalse();
        }

        [Fact]
        public async Task ForgotPassword_WhenEmailIsConfirmed_ReturnsNoContent()
        {
            // Arrange
            var user = await CreateUserAsync("Test Customer", "Customer", true);

            var request = new ForgotPasswordCommand(user.Email!);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the user's email is still confirmed.
            using var verificationScope = Factory.Services.CreateScope();

            var verificationUserManager = verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var persistedUser = await verificationUserManager.FindByIdAsync(user.Id.ToString());

            persistedUser.Should().NotBeNull();
            persistedUser!.EmailConfirmed.Should().BeTrue();
        }
    }
}
