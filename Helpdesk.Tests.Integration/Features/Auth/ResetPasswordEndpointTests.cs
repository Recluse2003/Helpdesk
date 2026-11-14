using FluentAssertions;
using Helpdesk.Api.Features.Auth.ResetPassword;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Helpdesk.Tests.Integration.Features.Auth
{
    public class ResetPasswordEndpointTests : IntegrationTestBase
    {
        public ResetPasswordEndpointTests(CustomWebApplicationFactory factory)
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

        private async Task<string> GenerateResetCodeAsync(ApplicationUser user)
        {
            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var trackedUser = await userManager.FindByIdAsync(user.Id.ToString());

            trackedUser.Should().NotBeNull();

            var identityCode = await userManager.GeneratePasswordResetTokenAsync(trackedUser!);

            return WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(identityCode));
        }

        [Fact]
        public async Task ResetPassword_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var request = new ResetPasswordRequest(
                Guid.NewGuid(),
                "some-reset-code",
                "NewSecurePassword123!");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/reset-password",
                request);

            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ResetPassword_WhenEmailIsNotConfirmed_ReturnsConflict()
        {
            // Arrange
            var user = await CreateUserAsync(
                "Unconfirmed Customer",
                "Customer");

            using (var scope = Factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider
                    .GetRequiredService<UserManager<ApplicationUser>>();

                var trackedUser = await userManager.FindByIdAsync(user.Id.ToString());

                trackedUser!.EmailConfirmed = false;

                var updateResult = await userManager.UpdateAsync(trackedUser);
                updateResult.Succeeded.Should().BeTrue();
            }

            var request = new ResetPasswordRequest(
                user.Id,
                "some-reset-code",
                "NewSecurePassword123!");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/reset-password",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task ResetPassword_WhenCodeIsMalformed_ReturnsConflict()
        {
            // Arrange
            var user = await CreateUserAsync(
                "Reset Customer",
                "Customer");

            var request = new ResetPasswordRequest(
                user.Id,
                "%%%invalid%%%",
                "NewSecurePassword123!");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/reset-password",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task ResetPassword_WhenCodeIsInvalidOrExpired_ReturnsConflict()
        {
            // Arrange
            var user = await CreateUserAsync(
                "Reset Customer",
                "Customer");

            var invalidCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("invalid-identity-token"));

            var request = new ResetPasswordRequest(
                user.Id,
                invalidCode,
                "NewSecurePassword123!");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/reset-password",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task ResetPassword_WhenNewPasswordDoesNotMeetRequirements_ReturnsBadRequest()
        {
            // Arrange
            var user = await CreateUserAsync(
                "Reset Customer",
                "Customer");

            var code = await GenerateResetCodeAsync(user);

            var request = new ResetPasswordRequest(
                user.Id,
                code,
                "short");

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/reset-password",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ResetPassword_WhenRequestIsValid_ReturnsNoContentAndChangesPassword()
        {
            // Arrange
            var user = await CreateUserAsync(
                "Reset Customer",
                "Customer");

            var newPassword = "NewSecurePassword123!";

            var code = await GenerateResetCodeAsync(user);

            var request = new ResetPasswordRequest(
                user.Id,
                code,
                newPassword);

            // Act
            var response = await Client.PostAsJsonAsync(
                "/api/auth/reset-password",
                request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var scope = Factory.Services.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var updatedUser = await userManager.FindByIdAsync(user.Id.ToString());

            updatedUser.Should().NotBeNull();

            var passwordIsValid = await userManager.CheckPasswordAsync(
                updatedUser!,
                newPassword);

            passwordIsValid.Should().BeTrue();
        }
    }
}
