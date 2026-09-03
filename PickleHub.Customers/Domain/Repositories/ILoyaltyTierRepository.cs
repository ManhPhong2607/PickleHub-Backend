using PickleHub.Customers.Domain.Entities;

namespace PickleHub.Customers.Domain.Repositories
{
    public interface ILoyaltyTierRepository
    {
        Task<List<LoyaltyTier>> GetAllOrderedAsync(CancellationToken ct = default);
        Task<LoyaltyTier?> GetByIdAsync(Guid id, CancellationToken ct = default);
        void Add(LoyaltyTier tier);
        void Update(LoyaltyTier tier);
        void Remove(LoyaltyTier tier);
    }
}
