using PickleHub.Blog.Domain.Entities;
using PickleHub.Blog.Domain.Enums;

namespace PickleHub.Blog.Domain.Repositories
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
        Task<List<Post>> GetRelatedAsync(Guid postId, Guid categoryId, int limit, CancellationToken ct = default);

        Task<Post?> GetPreviousPublishedAsync(Guid categoryId, DateTime publishedAt, CancellationToken ct = default);
        Task<Post?> GetNextPublishedAsync(Guid categoryId, DateTime publishedAt, CancellationToken ct = default);

        Task<(List<Post> Items, int TotalItems)> GetPublishedPagedAsync(
            string? keyword,
            Guid? categoryId,
            int page,
            int pageSize,
            CancellationToken ct = default);

        Task<(List<Post> Items, int TotalItems)> GetAdminPagedAsync(
            string? keyword,
            Guid? categoryId,
            PostStatus? status,
            int page,
            int pageSize,
            CancellationToken ct = default);

        void Add(Post post);
        void Update(Post post);
        void Remove(Post post);
    }
}
