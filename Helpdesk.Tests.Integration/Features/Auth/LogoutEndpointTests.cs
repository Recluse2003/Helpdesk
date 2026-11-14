using FluentAssertions;
using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Helpdesk.Tests.Integration.Features.Auth
{
    public class LogoutEndpointTests : IntegrationTestBase
    {
        public LogoutEndpointTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        private async Task<string> CreateRefreshTokenAsync()
        {
            using var scope = Factory.Services.CreateScope();

            var tokenService = scope.ServiceProvider
                .GetRequiredService<ITokenService>();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            var rawToken = tokenService.GenerateRefreshToken();
            var tokenHash = tokenService.HashRefreshToken(rawToken);

            var refreshToken = new RefreshToken(
                Guid.NewGuid(),
                tokenHash,
                DateTime.UtcNow.AddDays(7));

            context.RefreshTokens.Add(refreshToken);
            await context.SaveChangesAsync();

            return rawToken;
        }

        [Fact]
        public async Task Logout_WithValidRefreshCookie_ReturnsNoContentAndRevokesToken()
        {
            // Arrange
            var rawToken = await CreateRefreshTokenAsync();

            Assert.NotNull(rawToken);

            Client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={rawToken}");

            // Act
            var response = await Client.PostAsync("/api/auth/logout", content: null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<HelpdeskDbContext>();

            using var tokenScope = Factory.Services.CreateScope();

            var tokenService = tokenScope.ServiceProvider
                .GetRequiredService<ITokenService>();

            var tokenHash = tokenService.HashRefreshToken(rawToken);

            var storedToken = await context.RefreshTokens
                .SingleAsync(x => x.TokenHash == tokenHash);

            storedToken.IsRevoked.Should().BeTrue();

            response.Headers.TryGetValues("Set-Cookie", out var cookies)
                .Should().BeTrue();

            cookies!.Should().ContainSingle(x =>
                x.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase) &&
                x.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Logout_WithoutRefreshCookie_ReturnsNoContent()
        {
            // Act
            var response = await Client.PostAsync("/api/auth/logout", null);
            var body = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(
                HttpStatusCode.NoContent,
                $"the API returned: {body}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
