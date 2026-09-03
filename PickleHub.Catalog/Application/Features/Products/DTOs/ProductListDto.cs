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
        public bool IsOnSale { get; set; }
        public decimal? SalePercent { get; set; }
        public PromotionBadgeDto? ActivePromotion { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int SoldCount { get; set; }
        public string? Status { get; set; }
        public BrandDto? Brand { get; set; }
        public CategorySummaryDto? Category { get; set; }
    }

}
