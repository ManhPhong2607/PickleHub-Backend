using PickleHub.Catalog.Domain.Entities;

namespace PickleHub.Catalog.Domain.Repositories
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<List<Category>> GetAllAsync(CancellationToken ct = default);
        Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> HasChildrenAsync(Guid id, CancellationToken ct = default);
        Task<bool> HasProductsAsync(Guid id, CancellationToken ct = default);

        void Add(Category category);
        void Update(Category category);
        void Remove(Category category);
    }
}
