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

public class UserRegisteredConsumer(
    INotificationDbContext db,
    IEmailService emailService,
    IRateLimiterService rateLimiter,
    IHubContext<NotificationHub, INotificationClient> hubContext,
    ILogger<UserRegisteredConsumer> logger) : IConsumer<UserRegisteredEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId ?? Guid.NewGuid();

        logger.LogInformation("[UserRegisteredConsumer] Nhận UserRegisteredEvent cho Email [{Email}]", message.Email);

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
            Title = "Chào mừng bạn đến với PickleHub!",
            Content = "Tài khoản của bạn đã đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản (liên kết có hiệu lực trong 15 phút).",
            Type = NotificationType.System,
            ReferenceId = message.UserId,
            Action = "VERIFY_EMAIL",
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
            logger.LogError(ex, "Lỗi khi bắn SignalR push chào mừng cho User [{UserId}]", message.UserId);
        }

        // 4. Gửi Email Xác thực Đăng ký qua Resend
        if (!string.IsNullOrEmpty(message.Email))
        {
            var isLimited = await rateLimiter.IsRateLimitedAsync(message.Email, maxRequests: 5);
            if (!isLimited)
            {
                var template = await db.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Name == "UserRegistered");

                var subject = template?.Subject ?? "[PickleHub] Xác thực địa chỉ Email đăng ký tài khoản";

                var verifyUrl = !string.IsNullOrEmpty(message.VerificationUrl) 
                    ? message.VerificationUrl 
                    : $"https://picklehub.vn/auth/verify-email?token={message.VerificationToken}&email={message.Email}";

                var customerName = !string.IsNullOrEmpty(message.CustomerName) ? message.CustomerName : message.Email;

                var bodyHtml = template?.BodyHtml
                    .Replace("{{CustomerName}}", customerName)
                    .Replace("{{VerificationUrl}}", verifyUrl)
                    .Replace("{{VerificationToken}}", message.VerificationToken)
                    ?? $"<h2>Chào mừng {customerName} đến với PickleHub!</h2><p>Vui lòng nhấp vào liên kết sau để xác thực email của bạn:</p><p><a href='{verifyUrl}'>Xác thực tài khoản ngay</a></p>";

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
            EventType = nameof(UserRegisteredEvent),
            ConsumerName = nameof(UserRegisteredConsumer),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        logger.LogInformation("[UserRegisteredConsumer] Xử lý hoàn tất Email Đăng ký cho [{Email}]", message.Email);
    }
}
