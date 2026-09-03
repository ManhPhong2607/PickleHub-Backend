using Microsoft.EntityFrameworkCore;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Enums;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Infrastructure.Persistence.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly CatalogDbContext _db;
        public ProductRepository(CatalogDbContext db)
        {
            _db = db;
        }
        public void Add(Product product)
        {
            _db.Products.Add(product);
        }

        public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
        {
            var s = Slug.FromPersistedValue(slug);
            var query = _db.Products.Where(p => p.Slug == s);
            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);
            return await query.AnyAsync(ct);
        }
        // Query thẳng bảng product_variant (không qua Product.Variants navigation) vì cần check TOÀN BỘ variant trong hệ thống, không chỉ variant của 1 Product cụ thể.
        public async Task<bool> ExistsBySkuAsync(string sku, Guid? excludeVariantId = null, CancellationToken ct = default)
        {
            var query = _db.Set<ProductVariant>().Where(v => v.Sku == sku);
            if (excludeVariantId.HasValue)
                query = query.Where(v => v.Id != excludeVariantId.Value);
            return await query.AnyAsync(ct);
        }


        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Products.FindAsync([id], ct);
        }

        public async Task<Product?> GetByIdWithDetailAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            var s = Slug.FromPersistedValue(slug);
            return await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Slug == s, ct);
        }

        public async Task<(List<Product> Items, int TotalItems)> GetPagedAsync(
            string? keyword, Guid? categoryId, Guid? brandId, decimal? minPrice,
            decimal? maxPrice, SortBy sortBy, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => p.Status == ProductStatus.Active);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.Name.Contains(keyword));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.BasePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= maxPrice.Value);

            query = sortBy switch
            {
                SortBy.Newest => query.OrderByDescending(p => p.CreatedAt),
                SortBy.PriceAsc => query.OrderBy(p => p.BasePrice),
                SortBy.PriceDesc => query.OrderByDescending(p => p.BasePrice),
                SortBy.BestSelling => query.OrderByDescending(p => p.SoldCount),
                SortBy.MostViewed => query.OrderByDescending(p => p.ViewCount),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<(List<Product> Items, int TotalItems)> GetAdminPagedAsync(
            string? keyword, Guid? categoryId, Guid? brandId,
            decimal? minPrice, decimal? maxPrice, ProductStatus? status,
            SortBy sortBy, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.Name.Contains(keyword));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.BasePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= maxPrice.Value);

            query = sortBy switch
            {
                SortBy.Newest => query.OrderByDescending(p => p.CreatedAt),
                SortBy.PriceAsc => query.OrderBy(p => p.BasePrice),
                SortBy.PriceDesc => query.OrderByDescending(p => p.BasePrice),
                SortBy.BestSelling => query.OrderByDescending(p => p.SoldCount),
                SortBy.MostViewed => query.OrderByDescending(p => p.ViewCount),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public void Update(Product product)
        {
            _db.Products.Update(product);
        }


        // Sản phẩm liên quan: cùng danh mục, đang Active, loại trừ chính nó.
        // Ưu tiên bán chạy trước (tín hiệu mạnh nhất), sau đó tới lượt xem nhiều (tín hiệu quan tâm).
        public async Task<List<Product>> GetRelatedAsync(Guid productId, Guid categoryId, int limit, CancellationToken ct = default)
        {
            return await _db.Products
               .AsNoTracking()
               .Include(p => p.Category)
               .Include(p => p.Brand)
               .Include(p => p.Images)
               .Where(p => p.Status == ProductStatus.Active && p.CategoryId == categoryId && p.Id != productId)
               .OrderByDescending(p => p.SoldCount)
               .ThenByDescending(p => p.ViewCount)
               .Take(limit)
               .ToListAsync(ct);
        }

        public async Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default)
        {
            return await _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(ct);
        }

        public async Task<List<Product>> GetAllActiveWithStatsAsync(CancellationToken ct = default)
        {
            return await _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Where(p => p.Status == ProductStatus.Active)
                .ToListAsync(ct);
        }

        // Tăng +1 vào dòng của ngày hôm nay (UTC) cho sản phẩm này. Tạo dòng mới nếu chưa có.
        // Không gọi SaveChanges ở đây - để handler tự quyết định khi nào commit (thường gộp
        // chung 1 lần SaveChanges với việc tăng Product.ViewCount ở cùng use case).
        public async Task IncrementDailyViewAsync(Guid productId, CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var row = await _db.ProductViewDailies
                .FirstOrDefaultAsync(v => v.ProductId == productId && v.ViewDate == today, ct);

            if (row == null)
            {
                row = ProductViewDaily.Create(productId, today);
                _db.ProductViewDailies.Add(row);
            }
            row.IncrementView();

        }

        public async Task<List<ProductViewDaily>> GetViewDailyInRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
        {
            return await _db.ProductViewDailies
                .AsNoTracking()
                .Where(v => v.ViewDate >= fromDate && v.ViewDate <= toDate)
                .ToListAsync(ct);
        }


        // Dùng cho Inventory Service đồng bộ danh sách variant đầy đủ (kể cả variant chưa từng nhập kho lần nào) khi tạo template Excel nhập kho.
        public async Task<List<(Guid ProductId, Guid VariantId, string Sku)>> GetAllActiveVariantSummariesAsync(CancellationToken ct = default)
        {
            var rows = await _db.Products
                .AsNoTracking()
                .Where(p => p.Status == ProductStatus.Active)
                .SelectMany(p => p.Variants, (p, v) => new { p.Id, VariantId = v.Id, v.Sku })
                .ToListAsync(ct);

            return rows.Select(r => (r.Id, r.VariantId, r.Sku)).ToList();
        }
    }
}
