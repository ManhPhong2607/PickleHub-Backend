using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;
using PickleHub.CartOrder.Domain.Interfaces;
using PickleHub.Common.Events.Order;

namespace PickleHub.CartOrder.Application.Features.Orders.AdminCommands;

public record AdminCancelOrderCommand(
    Guid OrderId,
    string? Reason = null
) : IRequest<bool>;

public class AdminCancelOrderCommandHandler(
    ICartOrderDbContext db,
    ICustomerClient customerClient,
    IPublishEndpoint publishEndpoint
) : IRequestHandler<AdminCancelOrderCommand, bool>
{
    public async Task<bool> Handle(AdminCancelOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        if (order is null)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng yêu cầu.");
        }

        if (order.Status == OrderStatus.Completed)
        {
            throw new InvalidOperationException("Không thể hủy đơn hàng đã hoàn thành.");
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        order.CancelledBy = "Admin";
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

        await publishEndpoint.Publish(new OrderCancelledEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = customer?.FullName ?? order.ShippingFullName,
            CustomerEmail = customer?.Email ?? string.Empty,
            PreviousStatus = previousStatus,
            IsStockReserved = order.IsStockReserved,
            Items = eventItems,
            CancelledBy = "Admin",
            CancelReason = request.Reason,
            CancelledAt = DateTime.UtcNow
        }, ct);

        return true;
    }
}
