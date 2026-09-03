using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Enums;
using System.Globalization;

namespace PickleHub.Catalog.Domain.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id, CancellationToken ct =default);
        Task<Product?> GetByIdWithDetailAsync(Guid id, CancellationToken ct =default);
        Task<Product?> GetBySlugAsync(string slug, CancellationToken ct =default);
        Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct =default);
        Task<bool> ExistsBySkuAsync(string sku, Guid? excludeVariantId = null, CancellationToken ct = default);
        Task<List<Product>> GetRelatedAsync(Guid productId, Guid categoryId, int limit, CancellationToken ct = default);
        Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default);
        Task<List<Product>> GetAllActiveWithStatsAsync(CancellationToken ct = default);
        Task IncrementDailyViewAsync(Guid productId, CancellationToken ct = default);
        Task<List<ProductViewDaily>> GetViewDailyInRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);

        Task<List<(Guid ProductId, Guid VariantId, string Sku)>> GetAllActiveVariantSummariesAsync(CancellationToken ct = default);
        Task<(List<Product>Items, int TotalItems)> GetPagedAsync(
            string? keyword,
            Guid? categoryId,
            Guid? brandId,
            decimal? minPrice,
            decimal? maxPrice,
            SortBy sortBy,
            int page,
            int pageSize,
            CancellationToken ct = default); 
        Task<(List<Product>Items, int TotalItems)> GetAdminPagedAsync(
            string? keyword,
            Guid? categoryId,
            Guid? brandId,
            decimal? minPrice,
            decimal? maxPrice,
            ProductStatus? status,
            SortBy sortBy,
            int page,
            int pageSize,
            CancellationToken ct = default);


        void Add(Product product);
        void Update(Product product);
    }
}
