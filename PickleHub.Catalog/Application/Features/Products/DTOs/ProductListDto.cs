using PickleHub.Catalog.Application.Features.Brands.DTOs;
using PickleHub.Catalog.Application.Features.Categories.DTOs;

namespace PickleHub.Catalog.Application.Features.Products.DTOs
{
    public class ProductListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal EffectivePrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal EffectiveMinPrice { get; set; }
        public decimal EffectiveMaxPrice { get; set; }
        public bool IsSinglePrice => MinPrice == MaxPrice;
        public bool IsOnSale { get; set; }
        public decimal? SalePercent { get; set; }
        public PromotionBadgeDto? ActivePromotion { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int SoldCount { get; set; }
        public int ViewCount { get; set; }
        public string? Status { get; set; }
        public BrandDto? Brand { get; set; }
        public CategorySummaryDto? Category { get; set; }
        public List<ProductVariantDto> Variants { get; set; } = new();
        public List<ProductPromotionSummaryDto> Promotions { get; set; } = new();
    }

    public class ProductPromotionSummaryDto
    {
        public Guid PromotionId { get; set; }
        public string PromotionName { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; } = "Active"; // "Active", "Scheduled", "Expired"
    }
}
