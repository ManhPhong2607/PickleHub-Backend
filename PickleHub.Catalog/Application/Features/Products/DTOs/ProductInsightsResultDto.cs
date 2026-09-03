namespace PickleHub.Catalog.Application.Features.Products.DTOs
{
    public class ProductInsightsResultDto
    {

        // View cao, bán thấp -> có sức hút nhưng không chốt đơn, cần xem lại giá/ảnh/mô tả
        public List<ProductInsightItemDto> NeedsReview { get; set; } = new();

        // View cao, bán cao -> best-seller thật sự, ưu tiên đảm bảo đủ tồn kho
        public List<ProductInsightItemDto> BestSellers { get; set; } = new();

        // View thấp, bán cao -> khách quen mua lại không cần xem lại (VD: phụ kiện tiêu hao)
        public List<ProductInsightItemDto> RepeatBuys { get; set; } = new();

        // View thấp, bán thấp -> ế thật sự, cân nhắc ngừng bán/thanh lý
        public List<ProductInsightItemDto> SlowMovers { get; set; } = new();
    }
}
