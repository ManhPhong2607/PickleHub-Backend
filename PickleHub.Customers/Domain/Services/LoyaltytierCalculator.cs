using PickleHub.Customers.Domain.Entities;

namespace PickleHub.Customers.Domain.Services
{
    public static class LoyaltyTierCalculator
    {
        // Hạng hiện tại: hạng có MinSpend cao nhất mà TotalSpent vẫn đạt được.
        // Trả về null nếu khách chưa đạt hạng nào (dưới ngưỡng thấp nhất).
        public static LoyaltyTier? GetCurrentTier(decimal totalSpent, IEnumerable<LoyaltyTier> tiers)
        {
            return tiers
                .Where(t => totalSpent >= t.MinSpend)
                .OrderByDescending(t => t.MinSpend)
                .FirstOrDefault();
        }

        // Hạng tiếp theo: hạng có MinSpend thấp nhất trong số các hạng khách CHƯA đạt.
        public static LoyaltyTier? GetNextTier(decimal totalSpent, IEnumerable<LoyaltyTier> tiers)
        {
            return tiers
                .Where(t => totalSpent < t.MinSpend)
                .OrderBy(t => t.MinSpend)
                .FirstOrDefault();
        }
    }
}
