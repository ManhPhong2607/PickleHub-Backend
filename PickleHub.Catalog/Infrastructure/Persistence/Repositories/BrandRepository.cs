using Microsoft.EntityFrameworkCore;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.ValueObjects;
namespace PickleHub.Catalog.Infrastructure.Persistence.Repositories
{
    public class BrandRepository : IBrandRepository
    {
        private readonly CatalogDbContext _db;
        public BrandRepository(CatalogDbContext db) 
        {
            _db = db;
        }
        public void Add(Brand brand)
        {
            _db.Brands.Add(brand);
        }

        public async Task<List<Brand>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Brands.AsNoTracking()
                .OrderBy(b=> b.Name).ToListAsync(ct);              
        }

        public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Brands.FindAsync([id], ct);
        }

        public async Task<bool> HasProductsAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Products.AnyAsync(p=> p.BrandId == id, ct);
        }

        public async Task<Brand?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            var s = Slug.FromPersistedValue(slug);
            return await _db.Brands.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Slug.Value == slug, ct);
        }

        public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
        {
            var s = Slug.FromPersistedValue(slug);
            var query = _db.Brands.Where(b => b.Slug == s);
            if (excludeId.HasValue)
                query = query.Where(b => b.Id != excludeId.Value);
            return await query.AnyAsync(ct);
        }

        public void Remove(Brand brand)
        {
            _db.Brands.Remove(brand);
        }

        public void Update(Brand brand)
        {
            _db.Brands.Update(brand);
        }
    }
}
