namespace PickleHub.Catalog.Application.Features.Products.DTOs
{
    public class ProductImageDto
    {
        public Guid Id { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;    
        public Guid? VariantId { get; set; }
        public int SortOrder { get; set; }
        public bool IsSizeChart { get; set; }
    }
}
