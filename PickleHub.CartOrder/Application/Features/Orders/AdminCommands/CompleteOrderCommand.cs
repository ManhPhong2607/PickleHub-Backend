using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;
using PickleHub.CartOrder.Domain.Interfaces;
using PickleHub.Common.Events.Order;

namespace PickleHub.CartOrder.Application.Features.Orders.AdminCommands;

public record CompleteOrderCommand(Guid OrderId) : IRequest<bool>;

public class CompleteOrderCommandHandler(
    ICartOrderDbContext db,
    ICustomerClient customerClient,
    IPublishEndpoint publishEndpoint
) : IRequestHandler<CompleteOrderCommand, bool>
{
    public async Task<bool> Handle(CompleteOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order is null)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng yêu cầu.");
        }

        if (order.Status != OrderStatus.Shipping)
        {
            throw new InvalidOperationException("Chỉ có thể xác nhận hoàn thành cho đơn hàng đang ở trạng thái 'Shipping'.");
        }

        var oldStatus = order.Status;
        order.Status = OrderStatus.Completed;
        order.PaymentStatus = PaymentStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var customer = await customerClient.GetCustomerDetailsAsync(order.CustomerId, ct);

        await publishEndpoint.Publish(new OrderStatusUpdatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = customer?.FullName ?? order.ShippingFullName,
            CustomerEmail = customer?.Email ?? string.Empty,
            OldStatus = Enum.Parse<OrderStatus>(oldStatus.ToString(), true),
            NewStatus = Enum.Parse<OrderStatus>(order.Status.ToString(), true),
            TotalAmount = order.TotalAmount,
            Items = (order.Items ?? new List<Domain.Entities.OrderItem>()).Select(i => new OrderItemPayload
            {
                ProductId = i.ProductId,
                ProductVariantId = i.ProductVariantId,
                ProductNameSnapshot = i.ProductNameSnapshot,
                VariantAttributesSnapshot = i.VariantAttributesSnapshot,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            UpdatedAt = DateTime.UtcNow
        }, ct);

        return true;
    }
}
