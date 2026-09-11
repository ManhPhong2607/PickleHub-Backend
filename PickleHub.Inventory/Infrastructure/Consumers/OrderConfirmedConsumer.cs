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

            // Xử lý trừ tồn kho khi đơn hàng chuyển sang Confirmed, Shipping hoặc Completed (phòng trường hợp COD chuyển thẳng sang Shipping/Completed)
            if (message.NewStatus != OrderStatus.Confirmed &&
                message.NewStatus != OrderStatus.Shipping &&
                message.NewStatus != OrderStatus.Completed)
            {
                return;
            }

            if (message.Items == null || message.Items.Count == 0)
            {
                _logger.LogWarning("OrderStatusUpdatedEvent nhận được không có Items cho OrderId: {OrderId}. Bỏ qua trừ kho tự động.", message.OrderId);
                return;
            }

            _logger.LogInformation(
                "Đơn hàng ở trạng thái {Status}. Đang xử lý trừ kho (idempotent) cho OrderId: {OrderId}",
                message.NewStatus, message.OrderId);

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
