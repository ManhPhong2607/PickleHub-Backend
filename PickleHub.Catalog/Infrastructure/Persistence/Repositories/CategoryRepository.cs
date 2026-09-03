using Microsoft.EntityFrameworkCore;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.ValueObjects;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace PickleHub.Catalog.Infrastructure.Persistence.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly CatalogDbContext _db;
        public CategoryRepository(CatalogDbContext db) 
        {
            _db = db;
        }
        public void Add(Category category)
        {
            _db.Categories.Add(category);
        }

        public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
        {
            var s = Slug.FromPersistedValue(slug);
            var query = _db.Categories.Where(c => c.Slug == s);
            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);
            return await query.AnyAsync(ct);
        }

        public async Task<List<Category>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Categories.AsNoTracking().ToListAsync(ct);
        }

        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Categories.FindAsync([id], ct);
        }

        public async Task<bool> HasChildrenAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Categories.AnyAsync(c => c.ParentId == id, ct);
        }

        public async Task<bool> HasProductsAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Products.AnyAsync(p => p.CategoryId == id, ct);
        }

        public void Remove(Category category)
        {
            _db.Categories.Remove(category);
        }

        public void Update(Category category)
        {
            _db.Categories.Update(category);
        }
    }
}
