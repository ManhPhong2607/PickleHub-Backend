using MediatR;
using PickleHub.Inventory.Application.Common;
using PickleHub.Inventory.Domain.Enums;

namespace PickleHub.Inventory.Application.Features.Inventory.DeductStock
{
    public record DeductStockCommand(Guid OrderId, List<DeductStockItem> Items) : IRequest<DeductStockResult>;

    public record DeductStockItem(Guid ProductVariantId, int Quantity);

    public record DeductStockResult(
        bool Success,
        List<Guid> DepletedVariantIds,
        List<Guid> FailedVariantIds);

    public class DeductStockHandler : IRequestHandler<DeductStockCommand, DeductStockResult>
    {
        private readonly StockOperationExecutor _executor;
        private readonly ILogger<DeductStockHandler> _logger;

        public DeductStockHandler(StockOperationExecutor executor, ILogger<DeductStockHandler> logger)
        {
            _executor = executor;
            _logger = logger;
        }

        public async Task<DeductStockResult> Handle(DeductStockCommand request, CancellationToken ct)
        {
            var depletedVariantIds = new List<Guid>();
            var failedVariantIds = new List<Guid>();

            foreach (var item in request.Items)
            {
                var result = await _executor.ExecuteAsync(
                    item.ProductVariantId,
                    TransactionType.Deduct,
                    request.OrderId,
                    x => x.Deduct(item.Quantity, request.OrderId),
                    nameof(DeductStockCommand),
                    ct);

                // Chỉ tính "hết hàng" khi lần gọi này thực sự vừa deduct thành công.
                if (result.Applied && result.Item!.IsOutOfStock)
                    depletedVariantIds.Add(item.ProductVariantId);

                if (!result.Success)
                {
                    failedVariantIds.Add(item.ProductVariantId);
                    _logger.LogCritical(
                        "Deduct thất bại cho VariantId {VariantId}, OrderId {OrderId} sau khi đơn đã " +
                        "Confirmed. Kho CÓ THỂ đang lệch so với đơn hàng — cần kiểm tra/reconcile thủ công. " +
                        "Lý do: {Reason}",
                        item.ProductVariantId, request.OrderId, result.Message);
                }
            }

            return new DeductStockResult(failedVariantIds.Count == 0, depletedVariantIds, failedVariantIds);
        }
    }
}