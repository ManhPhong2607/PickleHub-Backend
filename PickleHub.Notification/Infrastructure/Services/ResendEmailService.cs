using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PickleHub.Notification.Infrastructure.Services;

public class ResendEmailService(HttpClient httpClient, IConfiguration config, ILogger<ResendEmailService> logger)
    : IEmailService
{
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string bodyHtml, CancellationToken ct = default)
    {
        var apiKey = config["Resend:ApiKey"];
        var fromEmail = config["Resend:FromEmail"] ?? "PickleHub <onboarding@resend.dev>";

        // Fallback môi trường Development: Nếu chưa cấu hình Resend API Key thật ➔ Ghi log nội dung ra Console để test local mượt mà
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("mock_dev_key") || !apiKey.StartsWith("re_"))
        {
            logger.LogInformation(@"
========= [LOCAL DEV EMAIL SIMULATION] =========
📧 To: {ToEmail}
🏷️ From: {FromEmail}
📌 Subject: {Subject}
📝 Content Length: {Length} characters
=================================================", toEmail, fromEmail, subject, bodyHtml.Length);

            return true;
        }

        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                from = fromEmail,
                to = new[] { toEmail },
                subject = subject,
                html = bodyHtml
            };

            requestMessage.Content = JsonContent.Create(payload);
            var response = await httpClient.SendAsync(requestMessage, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Gửi Email thành công qua Resend API tới: {ToEmail}", toEmail);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Thất bại khi gửi Email qua Resend API. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ngoại lệ xảy ra khi gửi Email tới {ToEmail}: {Message}", toEmail, ex.Message);
            return false;
        }
    }
}
