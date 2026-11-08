using Helpdesk.Api.Domain.Common;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Shared.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Helpdesk.Api.Features.Auth.ResendEmailConfirmation
{
    public class ResendEmailConfirmationCommandHandler : IRequestHandler<ResendEmailConfirmationCommand, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public ResendEmailConfirmationCommandHandler(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailService emailService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<Result> Handle(ResendEmailConfirmationCommand command, CancellationToken cancellationToken)
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
