using Microsoft.EntityFrameworkCore;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Enums;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Common
{
    // Success = có lỗi hay không, caller check field này để biết pass/fail.
    // Applied = lần gọi NÀY có thực sự vừa ghi dữ liệu không, hay bị skip vì idempotent
    //           (đã xử lý trước đó rồi) hoặc item không tồn tại nhưng coi là ok.
    // Item    = entity sau khi xử lý (null nếu thất bại thật hoặc not-found).
    // Message = lý do, chỉ có giá trị khi Success = false.
    public record StockOpResult(bool Success, bool Applied, InventoryItem? Item, string? Message);

    // Dùng chung cho Reserve, ReleaseReservation, Deduct, Return — 4 operations này
    // đều có cùng 1 khung xử lý: fetch item, check idempotency, đổi dữ liệu, lưu, retry nếu bị đụng độ đồng thời. 
    public class StockOperationExecutor
    {
        private const int MaxRetries = 3;

        private readonly IInventoryItemRepository _inventoryRepository;
        private readonly IStockTransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StockOperationExecutor> _logger;

        public StockOperationExecutor(
            IInventoryItemRepository inventoryRepository,
            IStockTransactionRepository transactionRepository,
            IUnitOfWork unitOfWork,
            ILogger<StockOperationExecutor> logger)
        {
            _inventoryRepository = inventoryRepository;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <param name="opName">Tên ngắn cho log, ví dụ "Reserve", "Deduct".</param>
        /// <param name="applyChange">Gọi đúng 1 domain method tương ứng, ví dụ:
        /// item => item.Reserve(quantity, orderId)</param>
        /// <param name="notFoundIsFailure">
        /// true (mặc định): không tìm thấy item = lỗi (Reserve/Deduct/Return — kho phải
        /// tồn tại mới xử lý được).
        /// false: dùng cho Release — nhả chỗ cho item không còn tồn tại thì coi như xong
        /// việc, không nên chặn luồng hủy đơn.</param>
        public async Task<StockOpResult> ExecuteAsync(
           Guid variantId,
           TransactionType type,
           Guid referenceId,
           Action<InventoryItem> applyChange,
           string opName,
           CancellationToken ct,
           bool notFoundIsFailure = true)
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var item = await _inventoryRepository.GetByVariantIdAsync(variantId, ct);

                    if (item is null)
                    {
                        if (notFoundIsFailure)
                        {
                            _logger.LogWarning(
                                "[{Op}] Không tìm thấy InventoryItem cho VariantId: {VariantId}",
                                opName, variantId);
                            return new StockOpResult(false, false, null, "Sản phẩm không tồn tại trong kho.");
                        }

                        _logger.LogWarning(
                            "[{Op}] Không tìm thấy InventoryItem cho VariantId: {VariantId} — bỏ qua.",
                            opName, variantId);
                        return new StockOpResult(true, false, null, null);
                    }

                    var alreadyDone = await _transactionRepository.ExistsAsync(item.Id, type, referenceId, ct);
                    if (alreadyDone)
                    {
                        _logger.LogInformation(
                            "[{Op}] Idempotency hit: VariantId {VariantId}, ReferenceId {ReferenceId}.",
                            opName, variantId, referenceId);
                        return new StockOpResult(true, false, item, null);
                    }

                    applyChange(item);

                    try
                    {
                        await _unitOfWork.SaveChangesAsync(ct);
                    }
                    catch (DuplicateOperationException)
                    {
                        _logger.LogInformation(
                            "[{Op}] Idempotency hit (backstop): VariantId {VariantId}, ReferenceId {ReferenceId}",
                            opName, variantId, referenceId);
                        return new StockOpResult(true, false, item, null);
                    }

                    _logger.LogInformation(
                        "[{Op}] Thành công cho VariantId: {VariantId}, ReferenceId: {ReferenceId}",
                        opName, variantId, referenceId);

                    return new StockOpResult(true, true, item, null);
                }
                catch (ConcurrencyConflictException)
                {
                    _logger.LogWarning(
                        "[{Op}] Xung đột đồng thời cho VariantId: {VariantId}. Lần thử {Attempt}/{MaxRetries}",
                        opName, variantId, attempt, MaxRetries);

                    _unitOfWork.ClearTracking();

                    if (attempt == MaxRetries)
                    {
                        _logger.LogError(
                            "[{Op}] Thất bại sau {MaxRetries} lần thử cho VariantId: {VariantId}",
                            opName, MaxRetries, variantId);
                        return new StockOpResult(false, false, null, "Hệ thống đang bận, vui lòng thử lại.");
                    }
                }
                catch (DomainException ex)
                {
                    _logger.LogInformation(
                        "[{Op}] Domain từ chối cho VariantId: {VariantId}. Lý do: {Reason}",
                        opName, variantId, ex.Message);
                    return new StockOpResult(false, false, null, ex.Message);
                }
            }

            return new StockOpResult(false, false, null, "Không thể xử lý tồn kho.");
        }
    }
}