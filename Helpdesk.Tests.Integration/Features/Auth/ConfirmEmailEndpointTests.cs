using FluentAssertions;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

namespace Helpdesk.Tests.Integration.Features.Auth
{
    public class ConfirmEmailEndpointTests : IntegrationTestBase
    {
        public ConfirmEmailEndpointTests(CustomWebApplicationFactory factory) : base(factory)
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
        public async Task ConfirmEmail_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("some-token"));

            // Act
            var response = await Client.PostAsync(
                $"/api/auth/confirm-email?userId={userId}&code={Uri.EscapeDataString(code)}",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ConfirmEmail_WhenCodeIsInvalid_ReturnsConflict()
        {
            // Arrange
            var user = await CreateUserAsync("Test Customer", "Customer");

            // Act
            var response = await Client.PostAsync(
                $"/api/auth/confirm-email?userId={user.Id}&code=*invalid*",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task ConfirmEmail_WhenEmailAlreadyConfirmed_ReturnsConflict()
        {
            // Arrange
            var user = await CreateUserAsync("Test Customer", "Customer");

            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var trackedUser = await userManager.FindByIdAsync(user.Id.ToString());

            trackedUser.Should().NotBeNull();

            trackedUser!.EmailConfirmed = true;

            var updateResult = await userManager.UpdateAsync(trackedUser);
            updateResult.Succeeded.Should().BeTrue();

            // Act
            var response = await Client.PostAsync(
                $"/api/auth/confirm-email?userId={user.Id}&code=some-code",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task ConfirmEmail_WhenCodeIsValid_ReturnsNoContent()
        {
            // Arrange
            var user = await CreateUserAsync("Test Customer", "Customer");

            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));

            // Act
            var response = await Client.PostAsync(
                $"/api/auth/confirm-email?userId={user.Id}&code={Uri.EscapeDataString(encodedCode)}",
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var confirmedUser = await userManager.FindByIdAsync(user.Id.ToString());

            confirmedUser!.EmailConfirmed.Should().BeTrue();
        }
    }
}
