using Microsoft.EntityFrameworkCore;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Enums;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Infrastructure.Persistence.Repositories
{
    public class StockTransactionRepository : IStockTransactionRepository
    {
        private readonly InventoryDbContext _db;

        public StockTransactionRepository(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<List<StockTransaction>> GetByInventoryItemAsync(Guid inventoryItemId, CancellationToken ct = default)
        {
            return await _db.StockTransactions
                .AsNoTracking()
                .Where(t => t.InventoryItemId == inventoryItemId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<bool> ExistsAsync(
            Guid inventoryItemId,
            TransactionType type,
            Guid referenceId,
            CancellationToken ct = default)
        {
            return await _db.StockTransactions
                .AsNoTracking()
                .AnyAsync(t => t.InventoryItemId == inventoryItemId
                            && t.Type == type
                            && t.ReferenceId == referenceId, ct);
        }
    }
}

