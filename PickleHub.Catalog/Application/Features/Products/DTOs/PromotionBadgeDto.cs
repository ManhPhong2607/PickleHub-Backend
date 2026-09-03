using PickleHub.Catalog.Domain.Enums;

namespace PickleHub.Catalog.Application.Features.Products.DTOs
{
    public class PromotionBadgeDto
    {
        public Guid PromotionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public decimal DiscountPercent { get; set; }
        
        // Cờ gốc dưới DB
        public bool IsActive { get; set; }
        
        // Trạng thái tính toán
        public PromotionStatus Status 
        { 
            get
            {
                if (!IsActive) return PromotionStatus.Disabled;
                
                var now = DateTime.UtcNow;
                if (now < StartsAt) return PromotionStatus.Scheduled;
                if (now > EndsAt) return PromotionStatus.Expired;
                
                return PromotionStatus.Active;
            }
        }
    }
}
