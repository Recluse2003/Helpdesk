using Helpdesk.Api.Infrastructure.Configuration;
using Helpdesk.Api.Shared.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Helpdesk.Api.Infrastructure.Services
{
    /// <summary>
    /// Sends emails using SMTP via MailKit.
    /// Reads configuration values such as SMTP server, port, and credentials from IConfiguration.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        /// <summary>
        /// Sends an email asynchronously using SMTP.
        /// </summary>
        /// <param name="email">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="htmlMessage">Email body, which is expected to be in HTML format.</param>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage, CancellationToken cancellationToken)
        {
            // Builds a email message
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_settings.From));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            // Set email body as HTML
            message.Body = new TextPart("html") { Text = htmlMessage };

            // Connect to SMTP server using TLS
            using var client = new SmtpClient();
            await client.ConnectAsync(
                _settings.SmtpServer,
                _settings.Port,
                MailKit.Security.SecureSocketOptions.StartTls,
                cancellationToken
            );

            // Authenticate using configured credentials
            await client.AuthenticateAsync(
                _settings.Username,
                _settings.Password,
                cancellationToken
            );

            // Send email
            await client.SendAsync(message, cancellationToken);

            // Disconnects from SMTP server
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}

