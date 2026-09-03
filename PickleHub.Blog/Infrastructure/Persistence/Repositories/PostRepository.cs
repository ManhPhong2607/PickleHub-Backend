using Microsoft.EntityFrameworkCore;
using PickleHub.Blog.Domain.Entities;
using PickleHub.Blog.Domain.Enums;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.ValueObjects;
using PickleHub.Blog.Infrastructure.Persistence;

namespace PickleHub.Blog.Infrastructure.Persistence.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly BlogDbContext _db;
        public PostRepository(BlogDbContext db)
        {
            _db = db;
        }

        public void Add(Post post)
        {
            _db.Posts.Add(post);
        }
        public void Update(Post post)
        {
            _db.Posts.Update(post);
        }
        public void Remove(Post post)
        {
            _db.Posts.Remove(post);
        }

        public async Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Posts.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            var s = Slug.FromPersistedValue(slug);
            return await _db.Posts.Include(p => p.Category).FirstOrDefaultAsync(p => p.Slug == s, ct);
        }
        public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
        {
            var s = Slug.FromPersistedValue(slug);
            var query = _db.Posts.Where(p => p.Slug == s);
            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }
            return await query.AnyAsync(ct);
        }
        public async Task<List<Post>> GetRelatedAsync(Guid postId, Guid categoryId, int limit, CancellationToken ct = default)
        {
            return await _db.Posts
                .AsNoTracking()
                .Where(p => p.CategoryId == categoryId && p.Id != postId && p.Status == PostStatus.Published)
                .OrderByDescending(p => p.PublishedAt)
                .Take(limit)
                .ToListAsync(ct);
        }
        public async Task<Post?> GetPreviousPublishedAsync(Guid categoryId, DateTime publishedAt, CancellationToken ct = default)
        {
            // Bài "trước đó" theo dòng thời gian đọc = bài cùng category, đăng sớm hơn bài hiện tại
            return await _db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published
                            && p.CategoryId == categoryId
                            && p.PublishedAt < publishedAt)
                .OrderByDescending(p => p.PublishedAt)
                .FirstOrDefaultAsync(ct);
        }
        public async Task<Post?> GetNextPublishedAsync(Guid categoryId, DateTime publishedAt, CancellationToken ct = default)
        {
            // Bài "tiếp theo" = bài cùng category, đăng muộn hơn bài hiện tại
            return await _db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published
                            && p.CategoryId == categoryId
                            && p.PublishedAt > publishedAt)
                .OrderBy(p => p.PublishedAt)
                .FirstOrDefaultAsync(ct);
        }
        public async Task<(List<Post> Items, int TotalItems)> GetPublishedPagedAsync(
           string? keyword,
           Guid? categoryId,
           int page,
           int pageSize,
           CancellationToken ct = default)
        {
            var query = _db.Posts.AsNoTracking().Include(p => p.Category).Where(p => p.Status == PostStatus.Published);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.Title.Contains(keyword));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
        public async Task<(List<Post> Items, int TotalItems)> GetAdminPagedAsync(
           string? keyword,
           Guid? categoryId,
           PostStatus? status,
           int page,
           int pageSize,
           CancellationToken ct = default)
        {
            var query = _db.Posts.AsNoTracking().Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.Title.Contains(keyword));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
