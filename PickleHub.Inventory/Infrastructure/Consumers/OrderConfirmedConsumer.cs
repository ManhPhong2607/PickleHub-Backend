using MassTransit;
using MediatR;
using PickleHub.Common.Enums;
using PickleHub.Common.Events.Inventory;
using PickleHub.Common.Events.Order;
using PickleHub.Common.Exceptions;
using PickleHub.Inventory.Application.Features.Inventory.DeductStock;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Infrastructure.Consumers
{
    public class OrderConfirmedConsumer : IConsumer<OrderStatusUpdatedEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<OrderConfirmedConsumer> _logger;

        public OrderConfirmedConsumer(
            ISender mediator,
            ILogger<OrderConfirmedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderStatusUpdatedEvent> context)
        {
            var message = context.Message;

            if (message.NewStatus != OrderStatus.Confirmed) return;

            _logger.LogInformation(
                "Đơn hàng đã được xác nhận. Đang trừ kho cho OrderId: {OrderId}",
                message.OrderId);

            var result = await _mediator.Send(new DeductStockCommand(
                message.OrderId,
                message.Items.Select(i => new DeductStockItem(
                    i.ProductVariantId,
                    i.Quantity)).ToList()),
                context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogCritical(
                    "Trừ kho THẤT BẠI cho {Count} item(s) trong OrderId: {OrderId}. VariantIds: {VariantIds}. " +
                    "Cần kiểm tra/reconcile thủ công.",
                    result.FailedVariantIds.Count, message.OrderId, string.Join(", ", result.FailedVariantIds));
            }
            // Publish StockDepletedEvent cho từng variant hết hàng
            foreach (var variantId in result.DepletedVariantIds)
            {
                await context.Publish(new StockDepletedEvent
                {
                    VariantId = variantId,
                    ConfirmedOrderId = message.OrderId,
                    OccurredAt = DateTime.UtcNow
                }, context.CancellationToken);

                _logger.LogInformation(
                    "Đã xuất bản sự kiện StockDepleted cho VariantId: {VariantId}", variantId);
            }
        }
    }
}
