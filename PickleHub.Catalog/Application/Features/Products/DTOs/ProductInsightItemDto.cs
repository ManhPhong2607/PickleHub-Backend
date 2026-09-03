namespace PickleHub.Catalog.Application.Features.Products.DTOs
{
    public class ProductInsightItemDto
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int ViewCount { get; set; }
        public int SoldCount { get; set; }

    }
}
