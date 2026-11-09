using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;

namespace Helpdesk.Api.Features.Auth.ForgotPassword
{
    /// <summary>
    /// Handles incoming forgot password commands. 
    /// </summary>
    /// <remarks>
    /// Retrieves the account associated with the supplied email address, verifies it exists and it's email has been confirmed, 
    /// before sending out an email to the supplied email address to reset the accounts password. 
    /// </remarks>
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public ForgotPasswordCommandHandler(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailService emailService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
        }

        /// <summary>
        /// Processes a forgot password command.
        /// </summary>
        /// <param name="request">Contains the email address assoicated with the account which is having a password reset.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A <see cref="Result"/> if successful, else returns a <see cref="Result"/> containing an <see cref="Error"/> 
        /// explaining why the request failed. 
        /// </returns>
        public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null || !user.EmailConfirmed)
                return Result.Success();

            // Generates a password reset token, and generates a reset password URL link containing it and the user's ID, before
            // sending to the account's email address. 
            var userId = user.Id.ToString();
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var resetLink = QueryHelpers.AddQueryString(
                $"{_configuration["Frontend:BaseUrl"]}/auth/reset-password",
                new Dictionary<string, string?>
                {
                    ["userId"] = userId,
                    ["code"] = code
                });

            var encodedLink = HtmlEncoder.Default.Encode(resetLink);

            var htmlMessage = $"""
                <p><a href="{encodedLink}">Reset your password</a></p>
                """;

            await _emailService.SendEmailAsync(
                user.Email!,
                "Reset your password",
                htmlMessage,
                cancellationToken);

            return Result.Success();
        }
    }
}
