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

public class OrderCreatedConsumer(
    INotificationDbContext db,
    IEmailService emailService,
    IRateLimiterService rateLimiter,
    IHubContext<NotificationHub, INotificationClient> hubContext,
    ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId ?? Guid.NewGuid();

        logger.LogInformation("[OrderCreatedConsumer] Nhận OrderCreatedEvent cho OrderId [{OrderId}]", message.OrderId);

        // 1. Idempotency Check (Chống duplicate event)
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId))
        {
            logger.LogWarning("Event [{EventId}] đã được xử lý trước đó. Bỏ qua.", eventId);
            return;
        }

        // 2. Tạo Web Notification (In-App)
        var webNoti = new WebNotification
        {
            UserId = message.CustomerId,
            Title = "Đặt hàng thành công",
            Content = $"Đơn hàng mã #{message.OrderId.ToString()[..8].ToUpper()} giá trị {message.TotalAmount:N0} VNĐ đã được tạo thành công.",
            Type = NotificationType.Order,
            ReferenceId = message.OrderId,
            Action = "VIEW_ORDER",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        db.WebNotifications.Add(webNoti);
        await db.SaveChangesAsync();

        // 3. Realtime SignalR Push xuống Browser của Customer
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

        // 4. Lấy Email Template từ DB & Gửi Email xác nhận đơn
        if (!string.IsNullOrEmpty(message.CustomerEmail))
        {
            var isLimited = await rateLimiter.IsRateLimitedAsync(message.CustomerEmail, maxRequests: 5);
            if (!isLimited)
            {
                var template = await db.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Name == "OrderConfirmation");

                var subject = template?.Subject.Replace("{{OrderCode}}", message.OrderId.ToString()[..8].ToUpper()) 
                              ?? $"[PickleHub] Xác nhận đơn hàng #{message.OrderId.ToString()[..8].ToUpper()}";

                var bodyHtml = template?.BodyHtml
                    .Replace("{{CustomerName}}", !string.IsNullOrEmpty(message.CustomerName) ? message.CustomerName : message.CustomerEmail)
                    .Replace("{{OrderCode}}", message.OrderId.ToString()[..8].ToUpper())
                    .Replace("{{TotalAmount}}", message.TotalAmount.ToString("N0"))
                    .Replace("{{ShippingAddress}}", message.ShippingAddress)
                    ?? $"<p>Cảm ơn bạn đã đặt hàng #{message.OrderId.ToString()[..8].ToUpper()} với tổng tiền {message.TotalAmount:N0} VNĐ.</p>";

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

        // 5. Ghi nhận Idempotency Event
        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            EventType = nameof(OrderCreatedEvent),
            ConsumerName = nameof(OrderCreatedConsumer),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        logger.LogInformation("[OrderCreatedConsumer] Xử lý hoàn tất OrderCreatedEvent cho Order [{OrderId}]", message.OrderId);
    }
}
