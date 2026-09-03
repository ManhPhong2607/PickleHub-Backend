using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickleHub.Common.Events.Authen;
using PickleHub.Notification.Application.Common.Interfaces;
using PickleHub.Notification.Domain.Entities;
using PickleHub.Notification.Domain.Enums;
using PickleHub.Notification.Infrastructure.Hubs;
using PickleHub.Notification.Infrastructure.Services;

namespace PickleHub.Notification.Infrastructure.Consumers;

public class PasswordResetRequestedConsumer(
    INotificationDbContext db,
    IEmailService emailService,
    IRateLimiterService rateLimiter,
    IHubContext<NotificationHub, INotificationClient> hubContext,
    ILogger<PasswordResetRequestedConsumer> logger) : IConsumer<PasswordResetRequestedEvent>
{
    public async Task Consume(ConsumeContext<PasswordResetRequestedEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId ?? Guid.NewGuid();

        logger.LogInformation("[PasswordResetRequestedConsumer] Nhận PasswordResetRequestedEvent cho Email [{Email}]", message.Email);

        // 1. Idempotency Check
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId))
        {
            logger.LogWarning("Event [{EventId}] đã được xử lý trước đó. Bỏ qua.", eventId);
            return;
        }

        // 2. Tạo Web Notification (In-App)
        var webNoti = new WebNotification
        {
            UserId = message.UserId,
            Title = "Yêu cầu đặt lại mật khẩu",
            Content = "Bạn vừa yêu cầu đặt lại mật khẩu. Vui lòng kiểm tra email để hoàn tất (liên kết có hiệu lực trong 15 phút).",
            Type = NotificationType.System,
            ReferenceId = message.UserId,
            Action = "RESET_PASSWORD",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        db.WebNotifications.Add(webNoti);
        await db.SaveChangesAsync();

        // 3. Realtime SignalR Push
        try
        {
            await hubContext.Clients.Group($"User_{message.UserId}").ReceiveNotification(new
            {
                webNoti.Id,
                webNoti.Title,
                webNoti.Content,
                Type = webNoti.Type.ToString(),
                webNoti.ReferenceId,
                webNoti.Action,
                webNoti.IsRead,
                webNoti.CreatedAt
            });

            var unreadCount = await db.WebNotifications
                .CountAsync(n => n.UserId == message.UserId && !n.IsRead);

            await hubContext.Clients.Group($"User_{message.UserId}").ReceiveUnreadCount(unreadCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi bắn SignalR push thông báo đổi mật khẩu cho User [{UserId}]", message.UserId);
        }

        // 4. Gửi Email Đặt lại / Thay đổi Mật khẩu qua Resend
        if (!string.IsNullOrEmpty(message.Email))
        {
            var isLimited = await rateLimiter.IsRateLimitedAsync(message.Email, maxRequests: 5);
            if (!isLimited)
            {
                var template = await db.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Name == "PasswordResetRequested");

                var subject = template?.Subject ?? "[PickleHub] Yêu cầu đặt lại mật khẩu tài khoản";

                var resetUrl = !string.IsNullOrEmpty(message.ResetUrl) 
                    ? message.ResetUrl 
                    : $"https://picklehub.vn/auth/reset-password?token={message.ResetToken}&email={message.Email}";

                var customerName = !string.IsNullOrEmpty(message.CustomerName) ? message.CustomerName : message.Email;

                var bodyHtml = template?.BodyHtml
                    .Replace("{{CustomerName}}", customerName)
                    .Replace("{{ResetUrl}}", resetUrl)
                    .Replace("{{ResetToken}}", message.ResetToken)
                    ?? $"<h2>Yêu cầu đặt lại mật khẩu cho {customerName}</h2><p>Vui lòng nhấp vào liên kết bên dưới để tạo mật khẩu mới:</p><p><a href='{resetUrl}'>Đặt lại mật khẩu</a></p><p>Liên kết này có hiệu lực trong 15 phút. Nếu bạn không gửi yêu cầu này, vui lòng bỏ qua email.</p>";

                var sentSuccess = await emailService.SendEmailAsync(message.Email, subject, bodyHtml);

                db.EmailLogs.Add(new EmailLog
                {
                    UserId = message.UserId,
                    EventId = eventId,
                    ToEmail = message.Email,
                    Subject = subject,
                    BodyHtml = bodyHtml,
                    Status = sentSuccess ? EmailStatus.Sent : EmailStatus.Failed,
                    SentAt = sentSuccess ? DateTime.UtcNow : null,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // 5. Ghi nhận Idempotency
        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            EventType = nameof(PasswordResetRequestedEvent),
            ConsumerName = nameof(PasswordResetRequestedConsumer),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        logger.LogInformation("[PasswordResetRequestedConsumer] Xử lý hoàn tất Email Đổi mật khẩu cho [{Email}]", message.Email);
    }
}
