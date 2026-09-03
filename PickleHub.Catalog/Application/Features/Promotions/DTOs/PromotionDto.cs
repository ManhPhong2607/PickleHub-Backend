namespace PickleHub.Catalog.Application.Features.Promotions.DTOs
{
    public class PromotionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public bool IsCurrentlyRunning { get; set; }
        public List<PromotionItemDto> Items { get; set; } = new();
    }

    public class PromotionItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class PromotionItemInput
    {
        public Guid ProductId { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    // Dùng cho danh sách (GetPromotionsQuery) - CHỈ đếm số sản phẩm, không load tên/ảnh
    // từng sản phẩm (khác PromotionDto.Items) để tránh phải query enrich cho mỗi promotion
    // trong trang danh sách - N+1 nếu danh sách có nhiều promotion.
    public class PromotionSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public bool IsCurrentlyRunning { get; set; }
        public int ProductCount { get; set; }
    }

    // Kết quả bulk-assign: báo rõ sản phẩm nào bị bỏ qua do overlap, giống pattern báo lỗi
    // từng dòng đã dùng ở tính năng import Excel - không chặn cả batch chỉ vì 1 vài sản phẩm lỗi.
    public class AssignProductsResultDto
    {
        public PromotionDto Promotion { get; set; } = null!;
        public int SuccessCount { get; set; }
        public List<Guid> ConflictingProductIds { get; set; } = new();
    }
}
