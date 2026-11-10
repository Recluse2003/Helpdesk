using Helpdesk.Api.Shared.Interfaces;

namespace Helpdesk.Tests.Integration
{
    public class FakeEmailService : IEmailService
    {
        private readonly List<SentEmail> _sentEmails = [];

        public IReadOnlyList<SentEmail> SentEmails => _sentEmails;

        public Task SendEmailAsync(string to, string subject, string htmlMessage, CancellationToken cancellationToken)
        {
            _sentEmails.Add(new SentEmail(to, subject, htmlMessage));

            return Task.CompletedTask;
        }

        public void Clear()
        {
            _sentEmails.Clear();
        }
    }

    public record SentEmail(string To, string Subject, string HtmlMessage);
}
