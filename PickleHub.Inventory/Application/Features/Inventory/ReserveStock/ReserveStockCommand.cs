using MassTransit;
using MediatR;
using PickleHub.Common.Events.Inventory;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Inventory.Application.Common;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Enums;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.ReserveStock
{
    // Gọi đồng bộ từ CartOrder lúc checkout (không phải qua RabbitMQ) - CartOrder cần biết ngay tại chỗ
    // có giữ được hàng hay không để quyết định trạng thái đơn.
    public record ReserveStockCommand(
        Guid VariantId,
        int Quantity,
        Guid OrderId
    ) : IRequest<ReserveStockResult>;

    public class ReserveStockHandler : IRequestHandler<ReserveStockCommand, ReserveStockResult>
    {
        private readonly StockOperationExecutor _executor;
        private readonly IPublishEndpoint _publishEndpoint;

        public ReserveStockHandler(StockOperationExecutor executor, IPublishEndpoint publishEndpoint)
        {
            _executor = executor;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ReserveStockResult> Handle(ReserveStockCommand request, CancellationToken ct)
        {
            var result = await _executor.ExecuteAsync(
                request.VariantId,
                TransactionType.Reserve,
                request.OrderId,
                item => item.Reserve(request.Quantity, request.OrderId),
                nameof(ReserveStockCommand),
                ct);

            if (result.Applied && result.Item!.IsLowStock)
            {
                await _publishEndpoint.Publish(new LowStockAlertEvent
                {
                    ProductVariantId = result.Item.ProductVariantId,
                    ProductId = result.Item.ProductId,
                    SkuSnapshot = result.Item.SkuSnapshot,
                    AvailableQuantity = result.Item.AvailableQuantity,
                    LowStockThreshold = result.Item.LowStockThreshold,
                    OccurredAt = DateTime.UtcNow
                }, ct);
            }

            return new ReserveStockResult(result.Success, result.Message);
        }
    }
}

