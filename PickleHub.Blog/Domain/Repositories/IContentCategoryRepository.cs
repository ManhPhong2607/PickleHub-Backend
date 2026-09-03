using PickleHub.Blog.Domain.Entities;

namespace PickleHub.Blog.Domain.Repositories
{
    public interface IContentCategoryRepository
    {
        Task<ContentCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ContentCategory?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<List<ContentCategory>> GetAllAsync(CancellationToken ct = default);
        Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> HasPostsAsync(Guid id, CancellationToken ct = default);
        void Add(ContentCategory category);
        void Update(ContentCategory category);
        void Remove(ContentCategory category);
    }
}
