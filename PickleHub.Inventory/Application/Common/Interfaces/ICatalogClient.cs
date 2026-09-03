namespace PickleHub.Inventory.Application.Common.Interfaces
{
    public interface ICatalogClient
    {
        Task<List<CatalogVariantDto>> GetAllVariantsAsync(CancellationToken ct = default);
    }

    public class CatalogVariantDto
    {
        public Guid ProductId { get; set; }
        public Guid VariantId { get; set; }
        public string Sku { get; set; } = string.Empty;
    }
}
