using MassTransit;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Domain.Interfaces;
using PickleHub.Common.Enums;
using PickleHub.Common.Events.Inventory;
using PickleHub.Common.Events.Order;

namespace PickleHub.CartOrder.Infrastructure.Consumers
{
    // Lắng nghe StockDepletedEvent từ Inventory Service.
    // Khi 1 variant vừa hết hàng (do 1 đơn khác được Confirmed và trừ hết kho) 
    // tự động hủy các đơn Pending khác đang chờ mà CHƯA giữ chỗ tồn kho (IsStockReserved = false)
    // và có chứa variant đó, để tránh khách phải chờ 1 đơn hàng chắc chắn không thể xử lý.
    public class StockDepletedConsumer(
        ICartOrderDbContext db,
        ICustomerClient customerClient,
        IPublishEndpoint publishEndpoint
    ) : IConsumer<StockDepletedEvent>
    {
        public async Task Consume(ConsumeContext<StockDepletedEvent> context)
        {
            var message = context.Message;

            // 1. Tìm các đơn Pending, CHƯA giữ chỗ tồn kho, có item trùng variant vừa hết hàng.
            //    Loại trừ chính đơn vừa được Confirmed (đơn gây ra sự kiện này).
            //    Một số item không có variant riêng -> ProductVariantId rỗng, dùng ProductId thay thế
            //    (đồng bộ với cách PaymentFailedConsumer đang xác định targetVariantId).
            var affectedOrders = await db.Orders
                .Include(o => o.Items)
                .Where(o => o.Id != message.ConfirmedOrderId
                    && o.Status == OrderStatus.Pending
                    && !o.IsStockReserved
                    && o.Items.Any(i =>
                        (i.ProductVariantId != Guid.Empty ? i.ProductVariantId : i.ProductId) == message.VariantId))
                .ToListAsync();

            if (affectedOrders.Count == 0)
            {
                return;
            }

            const string cancelReason = "Đơn hàng bị hủy tự động do sản phẩm trong đơn đã hết hàng.";

            foreach (var order in affectedOrders)
            {
                order.Status = OrderStatus.Cancelled;
                order.CancelledBy = "System";
                order.CancelReason = cancelReason;
                order.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            // 2. Với mỗi đơn vừa bị hủy -> publish OrderCancelledEvent để Notification gửi mail
            //    và AuditLog ghi log. PreviousStatus = Pending nên Inventory sẽ KHÔNG hoàn kho,
            //    vì đơn này chưa từng giữ chỗ tồn kho ngay từ đầu.
            foreach (var order in affectedOrders)
            {
                var customer = await customerClient.GetCustomerDetailsAsync(order.CustomerId);

                var eventItems = order.Items.Select(item => new OrderItemPayload
                {
                    ProductVariantId = item.ProductVariantId != Guid.Empty ? item.ProductVariantId : item.ProductId,
                    ProductNameSnapshot = item.ProductNameSnapshot,
                    VariantAttributesSnapshot = item.VariantAttributesSnapshot,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList();

                await publishEndpoint.Publish(new OrderCancelledEvent
                {
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    CustomerName = customer?.FullName ?? order.ShippingFullName,
                    CustomerEmail = customer?.Email ?? string.Empty,
                    PreviousStatus = OrderStatus.Pending,
                    IsStockReserved = order.IsStockReserved,
                    Items = eventItems,
                    CancelledBy = "System",
                    CancelReason = cancelReason,
                    CancelledAt = DateTime.UtcNow
                });
            }
        }
    }
}
