using Microsoft.EntityFrameworkCore;
using PickleHub.Blog.Domain.Entities;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.ValueObjects;
using PickleHub.Blog.Infrastructure.Persistence;

namespace PickleHub.Blog.Infrastructure.Persistence.Repositories
{
    public class ContentCategoryRepository : IContentCategoryRepository
    {
        private readonly BlogDbContext _db;
        public ContentCategoryRepository(BlogDbContext db)
        {
            _db = db;
        }
        public void Add(ContentCategory category)
        {
            _db.Categories.Add(category);
        }

        public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
        {
            var s = Slug.FromPersistedValue(slug);
            var query = _db.Categories.Where(c=> c.Slug == s);
            if(excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);
            return await query.AnyAsync(ct);
        }

        public async Task<List<ContentCategory>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Categories.AsNoTracking().OrderBy(c=>c.DisplayOrder).ToListAsync(ct);
        }

        public async Task<ContentCategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Categories.FindAsync([id], ct);
        }

        public async Task<ContentCategory?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            var s = Slug.FromPersistedValue(slug);
            return await _db.Categories.FirstOrDefaultAsync(c => c.Slug == s, ct);
        }

        public async Task<bool> HasPostsAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Posts.AnyAsync(p => p.CategoryId == id, ct);
        }

        public void Remove(ContentCategory category)
        {
            _db.Categories.Remove(category);
        }

        public void Update(ContentCategory category)
        {
            _db.Categories.Update(category);
        }
    }
}
