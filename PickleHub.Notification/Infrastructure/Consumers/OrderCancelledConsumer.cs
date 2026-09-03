using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Common.Events.Order;
using PickleHub.Notification.Application.Common.Interfaces;
using PickleHub.Notification.Domain.Entities;
using PickleHub.Notification.Domain.Enums;
using PickleHub.Notification.Infrastructure.Hubs;
using PickleHub.Notification.Infrastructure.Services;

namespace PickleHub.Notification.Infrastructure.Consumers
{
    public class OrderCancelledConsumer(
     INotificationDbContext db,
     IEmailService emailService,
     IRateLimiterService rateLimiter,
     IHubContext<NotificationHub, INotificationClient> hubContext,
     ILogger<OrderCancelledConsumer> logger) : IConsumer<OrderCancelledEvent>
    {
        public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
        {
            var message = context.Message;
            var eventId = context.MessageId ?? Guid.NewGuid();

            logger.LogInformation("[OrderCancelledConsumer] Nhận OrderCancelledEvent cho OrderId [{OrderId}] -> Hủy bởi: {CancelledBy}", message.OrderId, message.CancelledBy);

            // 1. Idempotency Check
            if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId))
            {
                logger.LogWarning("Event [{EventId}] đã được xử lý trước đó. Bỏ qua.", eventId);
                return;
            }

            var orderCodeStr = message.OrderId.ToString()[..8].ToUpper();
            var cancelledByLabel = message.CancelledBy switch
            {
                "Customer" => "bạn",
                "Admin" => "quản trị viên",
                "System" => "hệ thống (do sản phẩm hết hàng)",
                _ => message.CancelledBy
            };

            // 2. Tạo Web Notification (In-App)
            var webNoti = new WebNotification
            {
                UserId = message.CustomerId,
                Title = $"Đơn hàng #{orderCodeStr} đã bị hủy",
                Content = string.IsNullOrEmpty(message.CancelReason)
                    ? $"Đơn hàng của bạn đã bị hủy bởi {cancelledByLabel}."
                    : $"Đơn hàng của bạn đã bị hủy bởi {cancelledByLabel}. Lý do: {message.CancelReason}",
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

            // 4. Lấy Email Template & Gửi Email báo hủy đơn
            if (!string.IsNullOrEmpty(message.CustomerEmail))
            {
                var isLimited = await rateLimiter.IsRateLimitedAsync(message.CustomerEmail, maxRequests: 5);
                if (!isLimited)
                {
                    var template = await db.NotificationTemplates
                        .FirstOrDefaultAsync(t => t.Name == "OrderCancelled");

                    var subject = template?.Subject.Replace("{{OrderCode}}", orderCodeStr)
                                  ?? $"[PickleHub] Đơn hàng #{orderCodeStr} đã bị hủy";

                    var bodyHtml = template?.BodyHtml
                        .Replace("{{CustomerName}}", !string.IsNullOrEmpty(message.CustomerName) ? message.CustomerName : message.CustomerEmail)
                        .Replace("{{OrderCode}}", orderCodeStr)
                        .Replace("{{CancelledByLabel}}", cancelledByLabel)
                        .Replace("{{CancelReason}}", !string.IsNullOrEmpty(message.CancelReason) ? message.CancelReason : "Không có lý do cụ thể")
                        ?? $"<p>Đơn hàng #{orderCodeStr} của bạn đã bị hủy bởi {cancelledByLabel}.</p>";

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
                EventType = nameof(OrderCancelledEvent),
                ConsumerName = nameof(OrderCancelledConsumer),
                ProcessedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            logger.LogInformation("[OrderCancelledConsumer] Xử lý hoàn tất OrderCancelledEvent cho Order [{OrderId}]", message.OrderId);
        }
    }
}
