namespace PickleHub.Blog.Application.Common.Interfaces
{
    public interface ICatalogClient
    {
        Task<List<ProductSummary>>GetProductsByIdsAsync(List<Guid> productIds, CancellationToken ct = default);
    }

    public record ProductSummary(Guid id, string Name, string Slug, string? ImageUrl, decimal Price);

}
