namespace PickleHub.Catalog.Application.Features.Products.DTOs
{
    public class ProductVariantDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string AttributesJson { get; set; } = "{}";
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public decimal EffectivePrice { get; set; }
        public List<ProductImageDto> Images { get; set; } = [];
    }
}
