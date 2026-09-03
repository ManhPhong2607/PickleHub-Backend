using MassTransit;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;
using PickleHub.CartOrder.Domain.Interfaces;
using PickleHub.Common.Events.Order;
using PickleHub.Common.Events.Payment;

namespace PickleHub.CartOrder.Infrastructure.Consumers;

// Lắng nghe sự kiện PaymentCompletedEvent từ RabbitMQ.
// Khi thanh toán PayOS thành công -> Cập nhật PaymentStatus = Paid & Status = Confirmed (nếu đủ kho).
public class PaymentCompletedConsumer(
    ICartOrderDbContext db,
    ICustomerClient customerClient,
    IPublishEndpoint publishEndpoint
) : IConsumer<PaymentCompletedEvent>
{
    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var message = context.Message;

        // 1. Tìm đơn hàng tương ứng trong DB của CartOrder Service
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == message.OrderId);

        if (order is null || order.PaymentStatus == PaymentStatus.Paid)
        {
            return;
        }

        order.PaymentStatus = PaymentStatus.Paid;
        var oldStatus = order.Status;

        // 2. Nếu đơn hàng lúc Checkout đã được giữ chỗ tồn kho thành công -> Tự động chuyển Confirmed
        // Nếu lúc Checkout thiếu hàng (IsStockReserved = false) -> Giữ Pending để Admin xem xét duyệt thủ công
        if (order.IsStockReserved && order.Status == OrderStatus.Pending)
        {
            order.Status = OrderStatus.Confirmed;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // 4. Nếu chuyển sang Confirmed -> Publish OrderStatusUpdatedEvent để gửi email xác nhận đã thanh toán & xác nhận đơn
        if (order.Status == OrderStatus.Confirmed)
        {
            var customer = await customerClient.GetCustomerDetailsAsync(order.CustomerId);

            await publishEndpoint.Publish(new OrderStatusUpdatedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = customer?.FullName ?? order.ShippingFullName,
                CustomerEmail = customer?.Email ?? string.Empty,
                OldStatus = Enum.Parse<PickleHub.Common.Enums.OrderStatus>(oldStatus.ToString(), true),
                NewStatus = Enum.Parse<PickleHub.Common.Enums.OrderStatus>(order.Status.ToString(), true),
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
