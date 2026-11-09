using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Helpdesk.Api.Features.Auth.Register
{
    /// <summary>
    /// Handles incoming register new account commands. 
    /// </summary>
    /// <remarks>
    /// Verifies an account with the provided email address doesn't already exist, before attempting to create a new, unconfirmed
    /// account with the provided details. The new account is provided the customer role by default, and is not usable until 
    /// the email address has been confirmed. 
    /// </remarks>
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public RegisterCommandHandler(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailService emailService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
        }

        /// <summary>
        /// Processes a register new account request.
        /// </summary>
        /// <param name="confirm">Contains the new account's email, password, and display name.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> 
        /// explaining why the request failed. 
        /// </returns>
        public async Task<Result> Handle(RegisterCommand confirm, CancellationToken cancellationToken)
        {
            var existingUser = await _userManager.FindByEmailAsync(confirm.Email.Trim());

            if (existingUser != null)
                return Error.Conflict("Auth.EmailAlreadyRegistered", "An account with this email address already exists.");

            var user = new ApplicationUser
            {
                UserName = confirm.Email.Trim(),
                Email = confirm.Email.Trim(),
                DisplayName = confirm.DisplayName.Trim(),
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(user, confirm.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));

                return Error.Validation("Auth.RegistrationFailed", errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, RoleNames.Customer);

            if (!roleResult.Succeeded)
            {
                // Avoid leaving behind an account without its intended role.
                await _userManager.DeleteAsync(user);

                return Error.Failure("Auth.RoleAssignmentFailed", "Unable to assign the customer role.");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var confirmationLink = QueryHelpers.AddQueryString(
                $"{_configuration["Frontend:BaseUrl"]}/auth/confirm-email",
                new Dictionary<string, string?>
                {
                    ["userId"] = userId,
                    ["code"] = code
                });

            var htmlMessage = $"""
                <h2>Confirm your email address</h2>
                <p>Thanks for registering!</p>
                <p><a href="{confirmationLink}">Confirm your email</a></p>
                """;

            await _emailService.SendEmailAsync(
                user.Email!,
                "Confirm your email address",
                htmlMessage,
                cancellationToken);

            return Result.Success();
        }
    }
}
