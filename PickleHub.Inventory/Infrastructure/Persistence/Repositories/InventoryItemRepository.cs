using Microsoft.EntityFrameworkCore;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Infrastructure.Persistence.Repositories
{
    public class InventoryItemRepository : IInventoryItemRepository
    {
        private readonly InventoryDbContext _db;
        public InventoryItemRepository(InventoryDbContext db)
        {
            _db = db;
        }
        public async Task<bool> ExistsByVariantIdAsync(Guid variantId, CancellationToken ct = default)
        {
            return await _db.InventoryItems.AnyAsync(i => i.ProductVariantId == variantId, ct);
        }

        public async Task<InventoryItem?> GetByIdAsync(Guid inventoryItemId, CancellationToken ct = default)
        {
            return await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == inventoryItemId, ct);
        }

        public async Task<InventoryItem?> GetByVariantIdAsync(Guid variantId, CancellationToken ct = default)
        {
            return await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductVariantId == variantId, ct);
        }

        public async Task<InventoryItem?> GetByVariantIdWithTransactionsAsync(Guid variantId, CancellationToken ct = default)
        {
            return await _db.InventoryItems.Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.ProductVariantId == variantId, ct);
        }

        public async Task<List<InventoryItem>> GetLowStockItemsAsync(CancellationToken ct = default)
        {
            //  (AvailableQuantity = Quantity - ReservedQuantity) 
            return await _db.InventoryItems.AsNoTracking()
                .Where(i => i.AvailableQuantity > 0 && i.AvailableQuantity <= i.LowStockThreshold)
                .OrderBy(i => i.AvailableQuantity)
                .ToListAsync(ct);
        }

        public async Task<(List<InventoryItem> Items, int TotalItems)> GetPagedAsync(Guid? productId, bool? isLowStock, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.InventoryItems.AsNoTracking().AsQueryable();

            if (productId.HasValue)
                query = query.Where(i => i.ProductId == productId.Value);

            if (isLowStock == true)
                // AvailableQuantity = Quantity - ReservedQuantity (computed, không map DB nên phải inline)
                query = query.Where(i => (i.Quantity - i.ReservedQuantity) > 0
                                      && (i.Quantity - i.ReservedQuantity) <= i.LowStockThreshold);
            else if (isLowStock == false)
                query = query.Where(i => (i.Quantity - i.ReservedQuantity) > i.LowStockThreshold);

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(i => i.SkuSnapshot)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public void Add(InventoryItem item)
        {
            _db.InventoryItems.Add(item);
        }
        public void Update(InventoryItem item)
        {
           _db.InventoryItems.Update(item);
        }

        public void Remove(InventoryItem item)
        {
            _db.InventoryItems.Remove(item);
        }

        public async Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.InventoryItems.AsNoTracking()
                .OrderBy(i=> i.SkuSnapshot)
                .ToListAsync(ct);
        }
    }
}
