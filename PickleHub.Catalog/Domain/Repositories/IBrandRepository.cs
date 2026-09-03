using PickleHub.Catalog.Domain.Entities;

namespace PickleHub.Catalog.Domain.Repositories
{
    public interface IBrandRepository
    {
        Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<List<Brand>> GetAllAsync(CancellationToken ct = default);
        Task<bool> HasProductsAsync(Guid id, CancellationToken ct = default);
        Task<Brand?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
        void Add(Brand brand);
        void Update(Brand brand);
        void Remove(Brand brand);
    }
}
