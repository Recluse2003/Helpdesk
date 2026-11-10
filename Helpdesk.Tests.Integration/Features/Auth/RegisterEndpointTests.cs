using FluentAssertions;
using Helpdesk.Api.Features.Auth.Register;
using System.Net;
using System.Net.Http.Json;


namespace Helpdesk.Tests.Integration.Features.Auth
{
    public class RegisterEndpointTests : IntegrationTestBase
    {
        public RegisterEndpointTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Register_WithValidRequest_ReturnsNoContent()
        {

            // Arrange
            var request = new RegisterRequest(
                "test test",
                "test@test.com",
                "Password123!");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/register",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Register_WithInvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterRequest(
                "test test",
                "not-an-email",
                "Password123!");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/register",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithMissingDisplayName_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterRequest(
                "",
                "test@test.com",
                "Password123!");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/register",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithInvalidPassword_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterRequest(
                "test test",
                "test@test.com",
                "password");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/register",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithExistingEmail_ReturnsConflict()
        {
            // Arrange
            var request = new RegisterRequest(
                "test test",
                "test@test.com",
                "Password123!");

            // Register the account first.
            var firstResponse = await Client.PostAsJsonAsync(
                "/api/auth/register",
                request);

            firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act
            var secondResponse = await Client.PostAsJsonAsync(
                "/api/auth/register",
                request);

            // Assert
            secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }
}
