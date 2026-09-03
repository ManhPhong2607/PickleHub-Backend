using MassTransit;
using MediatR;
using PickleHub.Common.Enums;
using PickleHub.Common.Events.Order;
using PickleHub.Inventory.Application.Features.Inventory.ReleaseStock;
using PickleHub.Inventory.Application.Features.Inventory.ReturnStock;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Infrastructure.Consumers
{
    public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<OrderCancelledConsumer> _logger;

        public OrderCancelledConsumer(
            ISender mediator,
            ILogger<OrderCancelledConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
        {
            var message = context.Message;

            if (!message.IsStockReserved)
            {
                _logger.LogInformation(
                    "Đơn hàng {OrderId} bị hủy nhưng chưa từng giữ chỗ tồn kho (IsStockReserved = false). Bỏ qua xử lý kho.", 
                    message.OrderId);
                return;
            }

            // đơn hàng qua Confirmed/Shipping, hàng đã rời kho(deduct đã chạy) -> phải hoàn kho thật (return), cộng lại vào quantity 
            if (message.PreviousStatus == OrderStatus.Confirmed ||
                message.PreviousStatus == OrderStatus.Shipping)
            {
                _logger.LogInformation(
                    "Đơn hàng bị hủy sau khi đã xuất kho. Đang hoàn kho cho OrderId: {OrderId}", message.OrderId);

                await _mediator.Send(new ReturnStockCommand(
                    message.OrderId,
                    message.Items.Select(i => new ReturnStockItem(
                        i.ProductVariantId,
                        i.Quantity)).ToList()),
                    context.CancellationToken);
                return;
            }

            // đơn hàng đang pending -> chưa rời kho, đang giữ chỗ, chỉ cần nhả chỗ(releaseReservation), không đụng tới quantity
            if (message.PreviousStatus == OrderStatus.Pending)
            {
                _logger.LogInformation(
                    "Đơn hàng bị hủy khi còn Pending. Đang nhả chỗ tồn kho cho OrderId: {OrderId}", message.OrderId);

                var releaseItems = message.Items
                    .Select(i => new ReleaseStockItem(i.ProductVariantId, i.Quantity))
                    .ToList();

                await _mediator.Send(new ReleaseStockCommand(message.OrderId, releaseItems), context.CancellationToken);
                return;
            }
            _logger.LogInformation(
                    "Đơn hàng {OrderId} bị hủy từ trạng thái {Status}. Không cần xử lí tồn kho.", message.OrderId, message.PreviousStatus);

        }
    }
}
