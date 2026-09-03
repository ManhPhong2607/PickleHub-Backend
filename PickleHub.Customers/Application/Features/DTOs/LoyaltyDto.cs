namespace PickleHub.Customers.Application.Features.DTOs
{
    public class LoyaltyDto
    {
        public decimal TotalSpent { get; set; }

        // Null nếu khách chưa đạt hạng nào (dưới ngưỡng thấp nhất).
        public string? CurrentTierName { get; set; }
        public decimal CurrentDiscountPercent { get; set; }

        // Null nếu khách đã ở hạng cao nhất - không còn hạng tiếp theo để phấn đấu.
        public string? NextTierName { get; set; }
        public decimal? NextTierMinSpend { get; set; }
        public decimal? AmountNeededForNextTier { get; set; }

        public List<LoyaltyTierItemDto> AllTiers { get; set; } = new();
    }

    public class LoyaltyTierItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MinSpend { get; set; }
        public decimal DiscountPercent { get; set; }
        public List<string> Benefits { get; set; } = new();

        // Hạng khách hàng hiện đang ở - để FE tô sáng đúng tab như ảnh Alfaer.
        public bool IsCurrentTier { get; set; }

        // Khách đã đạt ngưỡng chi tiêu của hạng này chưa (dùng để FE hiện dấu tick/khóa).
        public bool IsAchieved { get; set; }
    }
}
