namespace PickleHub.Catalog.Application.Features.Products.DTOs
{
    public class TrendingProductDto
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int CurrentPeriodViews { get; set; }
        public int PreviousPeriodViews { get; set; }

        // Null khi kỳ trước = 0 view (không tính % được) -> xem field IsNewlyTrending thay vào đó.
        public decimal? GrowthPercent { get; set; }
        public bool IsNewLyTrending { get; set; } 
    }
}
