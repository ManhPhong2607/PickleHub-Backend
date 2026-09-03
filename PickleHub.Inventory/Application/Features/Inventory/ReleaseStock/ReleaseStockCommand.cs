using MediatR;
using PickleHub.Common.Interfaces;
using PickleHub.Inventory.Application.Common;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Enums;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.ReleaseStock
{
    public record ReleaseStockItem(Guid ProductVariantId, int Quantity);

    public record ReleaseStockCommand(Guid OrderId, List<ReleaseStockItem> Items) : IRequest<ReleaseStockResult>;

    public class ReleaseStockHandler : IRequestHandler<ReleaseStockCommand, ReleaseStockResult>
    {
        private readonly StockOperationExecutor _executor;

        public ReleaseStockHandler(StockOperationExecutor executor)
        {
            _executor = executor;
        }

        public async Task<ReleaseStockResult> Handle(ReleaseStockCommand request, CancellationToken ct)
        {
            foreach (var item in request.Items)
            {
                await _executor.ExecuteAsync(
                    item.ProductVariantId,
                    TransactionType.ReleaseReservation,
                    request.OrderId,
                    x => x.ReleaseReservation(item.Quantity, request.OrderId),
                    nameof(ReleaseStockCommand),
                    ct,
                    notFoundIsFailure: false); // nhả chỗ cho item không tồn tại vẫn coi là ok
            }

            return new ReleaseStockResult(true);
        }
    }        
}

