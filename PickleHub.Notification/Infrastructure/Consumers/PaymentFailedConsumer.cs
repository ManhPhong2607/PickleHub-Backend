using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Common.Events.Payment;
using PickleHub.Notification.Application.Common.Interfaces;
using PickleHub.Notification.Domain.Entities;
using PickleHub.Notification.Domain.Enums;
using PickleHub.Notification.Infrastructure.Hubs;

namespace PickleHub.Notification.Infrastructure.Consumers
{
    public class PaymentFailedConsumer(
       INotificationDbContext db,
       IHubContext<NotificationHub, INotificationClient> hubContext,
       ILogger<PaymentFailedConsumer> logger) : IConsumer<PaymentFailedEvent>
    {
        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            var message = context.Message;
            var eventId = context.MessageId ?? Guid.NewGuid();

            logger.LogInformation("[PaymentFailedConsumer] Nhận PaymentFailedEvent cho OrderId [{OrderId}]", message.OrderId);

            // 1. Idempotency Check
            if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId))
            {
                logger.LogWarning("Event [{EventId}] đã được xử lý trước đó. Bỏ qua.", eventId);
                return;
            }

            // 2. Tạo Web Notification (In-App) liên kết đúng với UserId
            var orderCodeStr = message.OrderId.ToString()[..8].ToUpper();
            var webNoti = new WebNotification
            {
                UserId = message.UserId,
                Title = "Thanh toán thất bại",
                Content = $"Giao dịch thanh toán cho đơn hàng #{orderCodeStr} không thành công. Lý do: {message.Reason}",
                Type = NotificationType.Payment,
                ReferenceId = message.OrderId,
                Action = "VIEW_ORDER",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            db.WebNotifications.Add(webNoti);
            await db.SaveChangesAsync();

            // 3. SignalR Targeted Push (Gửi đích danh cho Group User_{userId})
            try
            {
                if (message.UserId != Guid.Empty)
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi bắn SignalR push cho PaymentFailedEvent");
            }

            // 4. Ghi nhận Idempotency
            db.ProcessedEvents.Add(new ProcessedEvent
            {
                EventId = eventId,
                EventType = nameof(PaymentFailedEvent),
                ConsumerName = nameof(PaymentFailedConsumer),
                ProcessedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            logger.LogInformation("[PaymentFailedConsumer] Xử lý hoàn tất PaymentFailedEvent cho Order [{OrderId}]", message.OrderId);
        }
    }
}
