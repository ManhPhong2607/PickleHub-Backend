using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Enums;

namespace PickleHub.Inventory.Domain.Repositories
{
    public interface IStockTransactionRepository
    {
        Task<List<StockTransaction>> GetByInventoryItemAsync(Guid inventoryItemId, CancellationToken ct = default);

        /// <summary>
        /// Kiểm tra idempotency: đã có transaction cùng loại + cùng referenceId cho item này chưa.
        /// Dùng chung cho Reserve, ReleaseReservation, Deduct, Return.
        /// </summary>
        Task<bool> ExistsAsync(
            Guid inventoryItemId,
            TransactionType type,
            Guid referenceId,
            CancellationToken ct = default);
    }
}
