namespace PickleHub.Catalog.Application.Features.Products.DTOs
{
    public class VariantSyncDto
    {
        public Guid ProductId { get; set; }
        public Guid VariantId { get; set; }
        public string Sku { get; set; } = string.Empty;
    }
}
