using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickleHub.Common.Events.Order;
using PickleHub.Notification.Application.Common.Interfaces;
using PickleHub.Notification.Domain.Entities;
using PickleHub.Notification.Domain.Enums;
using PickleHub.Notification.Infrastructure.Hubs;
using PickleHub.Notification.Infrastructure.Persistence;
using PickleHub.Notification.Infrastructure.Services;

namespace PickleHub.Notification.Infrastructure.Consumers;

public class OrderStatusUpdatedConsumer(
    INotificationDbContext db,
    IEmailService emailService,
    IRateLimiterService rateLimiter,
    IHubContext<NotificationHub, INotificationClient> hubContext,
    ILogger<OrderStatusUpdatedConsumer> logger) : IConsumer<OrderStatusUpdatedEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusUpdatedEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId ?? Guid.NewGuid();

        logger.LogInformation("[OrderStatusUpdatedConsumer] Nhận OrderStatusUpdatedEvent cho OrderId [{OrderId}] -> Trạng thái mới: {NewStatus}", message.OrderId, message.NewStatus);

        // 1. Idempotency Check
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId))
        {
            logger.LogWarning("Event [{EventId}] đã được xử lý trước đó. Bỏ qua.", eventId);
            return;
        }

        // 2. Tạo Web Notification (In-App)
        var orderCodeStr = message.OrderId.ToString()[..8].ToUpper();
        var webNoti = new WebNotification
        {
            UserId = message.CustomerId,
            Title = $"Đơn hàng #{orderCodeStr} cập nhật trạng thái",
            Content = $"Đơn hàng của bạn vừa chuyển sang trạng thái: {message.NewStatus}.",
            Type = NotificationType.Order,
            ReferenceId = message.OrderId,
            Action = "VIEW_ORDER",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        db.WebNotifications.Add(webNoti);
        await db.SaveChangesAsync();

        // 3. Realtime SignalR Push
        try
        {
            await hubContext.Clients.Group($"User_{message.CustomerId}").ReceiveNotification(new
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
                .CountAsync(n => n.UserId == message.CustomerId && !n.IsRead);

            await hubContext.Clients.Group($"User_{message.CustomerId}").ReceiveUnreadCount(unreadCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi bắn SignalR push cho User [{UserId}]", message.CustomerId);
        }

        // 4. Lấy Email Template & Gửi Email cập nhật trạng thái
        if (!string.IsNullOrEmpty(message.CustomerEmail))
        {
            var isLimited = await rateLimiter.IsRateLimitedAsync(message.CustomerEmail, maxRequests: 5);
            if (!isLimited)
            {
                var template = await db.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Name == "OrderStatusUpdated");

                var subject = template?.Subject.Replace("{{OrderCode}}", orderCodeStr) 
                              ?? $"[PickleHub] Cập nhật trạng thái đơn hàng #{orderCodeStr}";

                var bodyHtml = template?.BodyHtml
                    .Replace("{{CustomerName}}", !string.IsNullOrEmpty(message.CustomerName) ? message.CustomerName : message.CustomerEmail)
                    .Replace("{{OrderCode}}", orderCodeStr)
                    .Replace("{{OrderStatusName}}", message.NewStatus.ToString())
                    .Replace("{{TrackingNumber}}", message.TrackingNumber ?? "N/A")
                    .Replace("{{TrackingUrl}}", message.TrackingUrl ?? "#")
                    ?? $"<p>Đơn hàng #{orderCodeStr} của bạn đã chuyển sang trạng thái: {message.NewStatus}.</p>";

                var sentSuccess = await emailService.SendEmailAsync(message.CustomerEmail, subject, bodyHtml);

                db.EmailLogs.Add(new EmailLog
                {
                    UserId = message.CustomerId,
                    EventId = eventId,
                    ToEmail = message.CustomerEmail,
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
            EventType = nameof(OrderStatusUpdatedEvent),
            ConsumerName = nameof(OrderStatusUpdatedConsumer),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        logger.LogInformation("[OrderStatusUpdatedConsumer] Xử lý hoàn tất OrderStatusUpdatedEvent cho Order [{OrderId}]", message.OrderId);
    }
}
