using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickleHub.Common.Events.Payment;
using PickleHub.Notification.Application.Common.Interfaces;
using PickleHub.Notification.Domain.Entities;
using PickleHub.Notification.Domain.Enums;
using PickleHub.Notification.Infrastructure.Hubs;

namespace PickleHub.Notification.Infrastructure.Consumers;

public class RefundCompletedConsumer(
    INotificationDbContext db,
    IHubContext<NotificationHub, INotificationClient> hubContext,
    ILogger<RefundCompletedConsumer> logger) : IConsumer<RefundCompletedEvent>
{
    public async Task Consume(ConsumeContext<RefundCompletedEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId ?? Guid.NewGuid();

        logger.LogInformation("[RefundCompletedConsumer] Nhận RefundCompletedEvent cho OrderId [{OrderId}], Số tiền: {Amount}", message.OrderId, message.Amount);

        // 1. Idempotency Check
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId))
        {
            logger.LogWarning("Event [{EventId}] đã được xử lý trước đó. Bỏ qua.", eventId);
            return;
        }

        // 2. Tạo Web Notification (In-App) liên kết đúng với UserId
        var orderCodeStr = message.OrderId.ToString()[..8].ToUpper();
        var refText = !string.IsNullOrWhiteSpace(message.BankTransactionReference) 
            ? $" (Mã GD: {message.BankTransactionReference})" 
            : "";

        var webNoti = new WebNotification
        {
            UserId = message.UserId,
            Title = "Hoàn tiền thành công",
            Content = $"PickleHub đã hoàn trả số tiền {message.Amount:N0} VNĐ cho đơn hàng #{orderCodeStr}{refText}. Vui lòng kiểm tra tài khoản ngân hàng của bạn.",
            Type = NotificationType.Payment,
            ReferenceId = message.OrderId,
            Action = "VIEW_ORDER",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        db.WebNotifications.Add(webNoti);

        // Đánh dấu EventId đã xử lý
        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        // 3. SignalR Targeted Push (Gửi realtime cho khách hàng)
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
                    webNoti.CreatedAt
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lỗi khi push SignalR realtime thông báo hoàn tiền cho User {UserId}", message.UserId);
        }
    }
}
