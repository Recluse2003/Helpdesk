using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Helpdesk.Api.Features.Auth.ResendEmailConfirmation
{
    /// <summary>
    /// Handles incoming resend account email confirmation requests. 
    /// </summary>
    /// <remarks>
    /// Verifies the account exists and hasn't already been verified using the provided email address, beforing sending an email
    /// with a link for confirming the account to the provided address. 
    /// </remarks>
    public class ResendConfirmationEmailCommandHandler : IRequestHandler<ResendConfirmationEmailCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public ResendConfirmationEmailCommandHandler(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailService emailService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
        }

        /// <summary>
        /// Processes a resend confirmation email command. 
        /// </summary>
        /// <param name="command">Contains the email address of the account requiring a new confirmation email.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> 
        /// explaining why the request failed. 
        /// </returns>
        public async Task<Result> Handle(ResendConfirmationEmailCommand command, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(command.Email);

            if (user == null)
                return Error.NotFound("User.NotFound", "User not found.");

            if (user.EmailConfirmed)
                return Error.Conflict("Auth.EmailAlreadyConfirmed", "This email address has already been confirmed.");

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
