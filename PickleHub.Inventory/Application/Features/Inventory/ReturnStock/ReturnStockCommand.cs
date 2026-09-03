using MediatR;
using PickleHub.Inventory.Application.Common;
using PickleHub.Inventory.Domain.Enums;

namespace PickleHub.Inventory.Application.Features.Inventory.ReturnStock
{
    public record ReturnStockItem(Guid ProductVariantId, int Quantity);

    public record ReturnStockCommand(Guid OrderId, List<ReturnStockItem> Items) : IRequest;

    public class ReturnStockHandler : IRequestHandler<ReturnStockCommand>
    {
        private readonly StockOperationExecutor _executor;

        public ReturnStockHandler(StockOperationExecutor executor)
        {
            _executor = executor;
        }

        public async Task Handle(ReturnStockCommand request, CancellationToken ct)
        {
            foreach (var item in request.Items)
            {
                await _executor.ExecuteAsync(
                    item.ProductVariantId,
                    TransactionType.Return,
                    request.OrderId,
                    x => x.Return(item.Quantity, request.OrderId),
                    nameof(ReturnStockCommand),
                    ct);
            }
        }
    }
}