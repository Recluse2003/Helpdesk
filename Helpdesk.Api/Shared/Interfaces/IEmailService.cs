namespace Helpdesk.Api.Shared.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage,
            CancellationToken cancellationToken = default);
    }
}
