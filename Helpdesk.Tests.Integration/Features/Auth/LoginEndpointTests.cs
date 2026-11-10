using FluentAssertions;
using Helpdesk.Api.Features.Auth.Login;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Helpdesk.Tests.Integration.Features.Auth
{
    public class LoginEndpointTests : IntegrationTestBase
    {
        public LoginEndpointTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        private async Task<ApplicationUser> CreateUserAsync(bool emailConfirmed = true)
        {
            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = "test@test.com",
                Email = "test@test.com",
                DisplayName = "Test User",
                EmailConfirmed = emailConfirmed
            };

            var result = await userManager.CreateAsync(user, "Password123!");

            result.Succeeded.Should().BeTrue(string.Join(", ", result.Errors.Select(x => x.Description)));

            return user;
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsAccessTokenAndRefreshCookie()
        {
            // Arrange
            await CreateUserAsync();

            var request = new LoginRequest(
                "test@test.com",
                "Password123!");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/login", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseBody = await response.Content.ReadFromJsonAsync<JsonElement>();

            responseBody.TryGetProperty("accessToken", out var accessToken).Should().BeTrue();

            accessToken.GetString().Should().NotBeNullOrWhiteSpace();

            // The refresh token should not be included in the response body.
            responseBody.TryGetProperty("refreshToken", out _).Should().BeFalse();

            // Check that the refresh token is delivered in a cookie.
            response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();

            var refreshCookie = cookies!.Single(x => x.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));

            refreshCookie.Should().Contain("httponly");
            refreshCookie.Should().Contain("secure");
            refreshCookie.Should().Contain("samesite=strict");
            refreshCookie.Should().Contain("path=/api/auth");
        }

        [Fact]
        public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest(
                "unknown@test.com",
                "Password123!");

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
        {
            // Arrange
            await CreateUserAsync();

            var request = new LoginRequest(
                "test@test.com",
                "WrongPassword123!");

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithUnconfirmedEmail_ReturnsConflict()
        {
            // Arrange
            await CreateUserAsync(emailConfirmed: false);

            var request = new LoginRequest(
                "test@test.com",
                "Password123!");

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Login_WithEmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoginRequest(
                "",
                "Password123!");

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_WithEmptyPassword_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoginRequest(
                "test@test.com",
                "");

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
