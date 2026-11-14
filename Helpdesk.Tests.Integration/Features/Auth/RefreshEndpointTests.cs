using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Features.Auth.Refresh;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Helpdesk.Tests.Integration.Features.Auth
{
    public class RefreshEndpointTests : IntegrationTestBase
    {
        public RefreshEndpointTests(CustomWebApplicationFactory factory) : base(factory)
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

        private static HttpRequestMessage CreateRefreshRequest(string refreshToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");

            request.Headers.Add("Cookie", $"refreshToken={refreshToken}");

            return request;
        }

        private static string ExtractCookieValue(string setCookie)
        {
            var encodedValue = setCookie
                .Split(';', 2)[0]
                .Split('=', 2)[1];

            return Uri.UnescapeDataString(encodedValue);
        }

        [Fact]
        public async Task Refresh_WhenRefreshCookieIsMissing_ReturnsUnauthorized()
        {
            // Act
            var response = await Client.PostAsync(
                "/api/auth/refresh", 
                null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WhenRefreshCookieIsEmpty_ReturnsUnauthorized()
        {
            // Arrange
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            request.Headers.Add("Cookie", "refreshToken=");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenIsInvalid_ReturnsUnauthorized()
        {
            // Arrange
            using var request = CreateRefreshRequest("invalid-refresh-token");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenIsValid_ReturnsNewAccessTokenAndRefreshCookie()
        {
            // Arrange
            var user = await CreateUserAsync("Refresh Customer", "Customer");

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

            var oldRefreshToken = "valid-refresh-token";

            var storedToken = new RefreshToken(
                user.Id,
                tokenService.HashRefreshToken(oldRefreshToken),
                DateTime.UtcNow.AddDays(7));

            context.RefreshTokens.Add(storedToken);
            await context.SaveChangesAsync();

            var storedTokenId = storedToken.Id;

            using var request = CreateRefreshRequest(oldRefreshToken);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();

            var refreshResponse = JsonSerializer.Deserialize<RefreshResponse>(
                responseContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            refreshResponse.Should().NotBeNull();
            refreshResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();

            // Verify that a replacement refresh cookie was issued.
            var setCookie = response.Headers.GetValues("Set-Cookie")
                .Single(x => x.StartsWith("refreshToken="));

            setCookie.Should().Contain("httponly");
            setCookie.Should().Contain("secure");
            setCookie.Should().Contain("samesite=strict");
            setCookie.Should().Contain("path=/api/auth");

            var newRefreshToken = ExtractCookieValue(setCookie);

            newRefreshToken.Should().NotBeNullOrWhiteSpace();
            newRefreshToken.Should().NotBe(oldRefreshToken);

            // Verify that the original token was revoked.
            var oldStoredToken = await context.RefreshTokens
                .AsNoTracking()
                .SingleAsync(x => x.Id == storedTokenId);

            oldStoredToken.RevokedAt.Should().NotBeNull();

            // Verify that the replacement token was stored.
            var replacement = await context.RefreshTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.TokenHash == tokenService.HashRefreshToken(newRefreshToken));

            replacement.Should().NotBeNull();
            replacement!.UserId.Should().Be(user.Id);
            replacement.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Refresh_WhenRefreshTokenIsReused_ReturnsUnauthorized()
        {
            // Arrange
            var user = await CreateUserAsync("Refresh Customer", "Customer");

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var tokenService = scope.ServiceProvider
                .GetRequiredService<ITokenService>();

            var oldRefreshToken = "single-use-refresh-token";

            context.RefreshTokens.Add(new RefreshToken(
                user.Id,
                tokenService.HashRefreshToken(oldRefreshToken),
                DateTime.UtcNow.AddDays(7)));

            await context.SaveChangesAsync();

            // First request rotates the token.
            using var firstRequest = CreateRefreshRequest(oldRefreshToken);
            var firstResponse = await Client.SendAsync(firstRequest);

            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Act - reuse the original token.
            using var secondRequest = CreateRefreshRequest(oldRefreshToken);
            var secondResponse = await Client.SendAsync(secondRequest);

            // Assert
            secondResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
