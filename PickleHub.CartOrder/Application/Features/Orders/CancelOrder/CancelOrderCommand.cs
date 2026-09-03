using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;
using PickleHub.CartOrder.Domain.Interfaces;
using PickleHub.Common.Events.Order;

namespace PickleHub.CartOrder.Application.Features.Orders.CancelOrder;

// Command hủy đơn hàng.
public record CancelOrderCommand(
    Guid OrderId,
    Guid UserId,
    string? Reason = null
) : IRequest<bool>;

public class CancelOrderCommandHandler(
    ICartOrderDbContext db,
    ICustomerClient customerClient,
    IPublishEndpoint publishEndpoint
) : IRequestHandler<CancelOrderCommand, bool>
{
    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == request.UserId, ct);

        if (order is null)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng yêu cầu hoặc bạn không có quyền hủy đơn này.");
        }

        // Chỉ hủy được khi status là Pending hoặc Confirmed
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
        {
            throw new InvalidOperationException("Không thể hủy đơn hàng ở trạng thái hiện tại.");
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        order.CancelledBy = "Customer";
        order.CancelReason = request.Reason;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var customer = await customerClient.GetCustomerDetailsAsync(order.CustomerId, ct);

        var eventItems = order.Items.Select(item => new OrderItemPayload
        {
            ProductVariantId = item.ProductVariantId != Guid.Empty ? item.ProductVariantId : item.ProductId,
            ProductNameSnapshot = item.ProductNameSnapshot,
            VariantAttributesSnapshot = item.VariantAttributesSnapshot,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }).ToList();

        // Publish Event "OrderCancelled"
        await publishEndpoint.Publish(new OrderCancelledEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = customer?.FullName ?? order.ShippingFullName,
            CustomerEmail = customer?.Email ?? string.Empty,
            PreviousStatus = previousStatus,
            IsStockReserved = order.IsStockReserved,
            Items = eventItems,
            CancelledBy = "Customer",
            CancelReason = request.Reason,
            CancelledAt = DateTime.UtcNow
        }, ct);

        return true;
    }
}
