using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.Common.Enums;
using PickleHub.CartOrder.Domain.Interfaces;
using PickleHub.Common.Events.Order;

namespace PickleHub.CartOrder.Application.Features.Orders.AdminCommands;

public record ConfirmOrderCommand(Guid OrderId) : IRequest<bool>;

public class ConfirmOrderCommandHandler(
    ICartOrderDbContext db,
    ICustomerClient customerClient,
    IPublishEndpoint publishEndpoint
) : IRequestHandler<ConfirmOrderCommand, bool>
{
    public async Task<bool> Handle(ConfirmOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order == null)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn hàng yêu cầu.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Không thể xác nhận đơn hàng đang ở trạng thái '{order.Status}'.");
        }

        var customer = await customerClient.GetCustomerDetailsAsync(order.CustomerId, ct);
        var oldStatus = order.Status;
        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await publishEndpoint.Publish(new OrderStatusUpdatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = customer?.FullName ?? order.ShippingFullName,
            CustomerEmail = customer?.Email ?? string.Empty,
            OldStatus = Enum.Parse<OrderStatus>(oldStatus.ToString(), true),
            NewStatus = Enum.Parse<OrderStatus>(order.Status.ToString(), true),
            UpdatedAt = DateTime.UtcNow
        }, ct);

        return true;
    }
}
