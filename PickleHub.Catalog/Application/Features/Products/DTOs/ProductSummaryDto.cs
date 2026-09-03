namespace PickleHub.Catalog.Application.Features.Products.DTOs
{
    public class ProductSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
    }
}
