using System;
using System.Threading;
using System.Threading.Tasks;

namespace PickleHub.CartOrder.Domain.Interfaces;

// Contract để gọi Inventory Service để quản lý tồn kho đồng bộ, chống race condition.
public interface IInventoryClient
{
    Task<bool> CheckStockAsync(Guid variantId, int quantity, CancellationToken ct = default);
    Task<bool> ReserveStockAsync(Guid variantId, int quantity, CancellationToken ct = default);
    Task<bool> ReleaseStockAsync(Guid variantId, int quantity, CancellationToken ct = default);
}
