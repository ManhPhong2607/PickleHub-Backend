using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Common.Events.Inventory;
using PickleHub.Notification.Application.Common.Interfaces;
using PickleHub.Notification.Domain.Entities;
using PickleHub.Notification.Domain.Enums;
using PickleHub.Notification.Infrastructure.Hubs;

namespace PickleHub.Notification.Infrastructure.Consumers
{
    public class LowStockAlertConsumer(
        INotificationDbContext db,
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<LowStockAlertConsumer> logger) : IConsumer<LowStockAlertEvent>
    {
        public async Task Consume(ConsumeContext<LowStockAlertEvent> context)
        {
            var message = context.Message;
            var eventId = context.MessageId ?? Guid.NewGuid();

            logger.LogInformation(
                "[LowStockAlertConsumer] Nhận LowStockAlertEvent cho VariantId [{VariantId}] - SKU: {Sku} - Còn: {Qty}",
                message.ProductVariantId, message.SkuSnapshot, message.AvailableQuantity);

            // 1. Idempotency Check
            if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId))
            {
                logger.LogWarning("Event [{EventId}] đã được xử lý trước đó. Bỏ qua.", eventId);
                return;
            }

            // 2. Tạo Web Notification dạng System — UserId = Guid.Empty (không gắn với 1 user cụ thể,
            //    admin dashboard sẽ query theo Type = System để hiển thị alert chung)
            var isDepleted = message.AvailableQuantity == 0;
            var webNoti = new WebNotification
            {
                UserId = Guid.Empty,
                Title = isDepleted ? $"Hết hàng: {message.SkuSnapshot}" : $"Sắp hết hàng: {message.SkuSnapshot}",
                Content = isDepleted
                    ? $"SKU {message.SkuSnapshot} đã hết hàng khả dụng. Vui lòng nhập thêm hàng."
                    : $"SKU {message.SkuSnapshot} còn {message.AvailableQuantity} đơn vị (ngưỡng: {message.LowStockThreshold}). Cân nhắc nhập thêm hàng.",
                Type = NotificationType.System,
                ReferenceId = message.ProductVariantId,
                Action = "VIEW_INVENTORY",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            db.WebNotifications.Add(webNoti);
            await db.SaveChangesAsync();

            // 3. SignalR Push tới tất cả admin đang online (group "Admins" — xem NotificationHub)
            try
            {
                await hubContext.Clients.Group("Admins").ReceiveNotification(new
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[LowStockAlertConsumer] Lỗi khi bắn SignalR push tới group Admins");
            }

            // 4. Ghi nhận Idempotency
            db.ProcessedEvents.Add(new ProcessedEvent
            {
                EventId = eventId,
                EventType = nameof(LowStockAlertEvent),
                ConsumerName = nameof(LowStockAlertConsumer),
                ProcessedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            logger.LogInformation(
                "[LowStockAlertConsumer] Xử lý hoàn tất LowStockAlertEvent cho VariantId [{VariantId}]",
                message.ProductVariantId);
        }
    }
}
