namespace PickleHub.Notification.Infrastructure.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string bodyHtml, CancellationToken ct = default);
}
