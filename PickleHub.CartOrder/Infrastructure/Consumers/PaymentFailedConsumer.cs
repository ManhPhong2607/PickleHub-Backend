using MassTransit;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;
using PickleHub.CartOrder.Domain.Interfaces;
using PickleHub.Common.Events.Order;
using PickleHub.Common.Events.Payment;

namespace PickleHub.CartOrder.Infrastructure.Consumers;

public class PaymentFailedConsumer(
    ICartOrderDbContext db,
    IInventoryClient inventoryClient,
    ICustomerClient customerClient,
    IPublishEndpoint publishEndpoint
) : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;

        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == message.OrderId);

        if (order is null || order.PaymentStatus == PaymentStatus.Failed || order.PaymentStatus == PaymentStatus.Paid)
        {
            return;
        }

        var oldStatus = order.Status;

        // 1. Cập nhật trạng thái thanh toán thất bại, tự động hủy đơn
        order.PaymentStatus = PaymentStatus.Failed;
        order.Status = OrderStatus.Cancelled;
        order.CancelledBy = "System";
        order.CancelReason = "Hủy đơn tự động do thanh toán thất bại hoặc quá hạn.";
        order.UpdatedAt = DateTime.UtcNow;

        // 2. Giải phóng tồn kho đã giữ chỗ (Release) đồng bộ nếu lúc Checkout giữ chỗ thành công
        if (order.IsStockReserved)
        {
            foreach (var item in order.Items)
            {
                var targetVariantId = item.ProductVariantId != Guid.Empty ? item.ProductVariantId : item.ProductId;
                await inventoryClient.ReleaseStockAsync(targetVariantId, item.Quantity, order.Id);
            }
            order.IsStockReserved = false;
        }

        await db.SaveChangesAsync();

        // 3. Phát sự kiện OrderStatusUpdatedEvent để gửi email thông báo hủy đơn
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
