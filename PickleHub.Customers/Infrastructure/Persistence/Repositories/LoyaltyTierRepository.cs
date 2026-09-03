using PickleHub.Customers.Domain.Entities;
using PickleHub.Customers.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using PickleHub.Customers.Infrastructure.Persistence;
namespace PickleHub.Customers.Infrastructure.Persistence.Repositories
{
    public class LoyaltyTierRepository : ILoyaltyTierRepository
    {
        private readonly CustomerDbContext _db;
        public LoyaltyTierRepository(CustomerDbContext db)
        {
            _db = db;
        }
        public void Add(LoyaltyTier tier)
        {
           _db.LoyaltyTiers.Add(tier);
        }

        public async Task<List<LoyaltyTier>> GetAllOrderedAsync(CancellationToken ct = default)
        {
            return await _db.LoyaltyTiers.AsNoTracking()
                .OrderBy(t => t.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<LoyaltyTier?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.LoyaltyTiers.FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public void Remove(LoyaltyTier tier)
        {
            _db.LoyaltyTiers.Remove(tier);
        }

        public void Update(LoyaltyTier tier)
        {
            _db.LoyaltyTiers.Update(tier);
        }
    }
}
