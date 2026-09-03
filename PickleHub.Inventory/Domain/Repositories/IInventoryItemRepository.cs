using PickleHub.Inventory.Domain.Entities;

namespace PickleHub.Inventory.Domain.Repositories
{
    public interface IInventoryItemRepository
    {
        Task<InventoryItem?> GetByIdAsync(Guid inventoryItemId, CancellationToken ct = default);
        Task<InventoryItem?> GetByVariantIdAsync(Guid variantId, CancellationToken ct = default);
        Task<InventoryItem?> GetByVariantIdWithTransactionsAsync(Guid variantId, CancellationToken ct = default);
        Task<bool> ExistsByVariantIdAsync(Guid variantId, CancellationToken ct = default);
        Task<(List<InventoryItem> Items, int TotalItems)> GetPagedAsync(
            Guid? productId,
            bool? isLowStock,
            int page,
            int pageSize,
            CancellationToken ct = default);
        Task<List<InventoryItem>> GetLowStockItemsAsync(CancellationToken ct = default);
        Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default);
        void Add(InventoryItem item);
        void Update(InventoryItem item);
        void Remove(InventoryItem item);
    }
}
