using Microsoft.EntityFrameworkCore;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Enums;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Infrastructure.Persistence.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly CatalogDbContext _db;

        public PromotionRepository(CatalogDbContext db)
        {
            _db = db;
        }

        public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Promotions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<(List<Promotion> Items, int TotalItems)> GetPagedAsync(PromotionStatus? status, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Promotions.Include(p => p.Items).AsQueryable();

            if (status.HasValue)
            {
                var now = DateTime.UtcNow;
                switch (status.Value)
                {
                    case PromotionStatus.Active:
                        query = query.Where(p => p.IsActive && p.StartsAt <= now && p.EndsAt >= now);
                        break;
                    case PromotionStatus.Scheduled:
                        query = query.Where(p => p.IsActive && p.StartsAt > now);
                        break;
                    case PromotionStatus.Expired:
                        query = query.Where(p => p.EndsAt < now);
                        break;
                    case PromotionStatus.Disabled:
                        query = query.Where(p => !p.IsActive);
                        break;
                }
            }

            query = query.OrderByDescending(p => p.StartsAt);

            var totalItems = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalItems);
        }

        // Chỉ coi là conflict khi overlap ngày VÀ CÙNG Priority - khác Priority thì cho phép
        // chồng lấn tự do, đây chính là điểm khác so với bản chặn cứng trước đây.
        public async Task<HashSet<Guid>> GetConflictingProductIdsAsync(
            List<Guid> productIds,
            DateTime startsAt,
            DateTime endsAt,
            int priority,
            Guid? promotionIdToExclude,
            CancellationToken ct = default)
        {
            if (productIds.Count == 0) return new HashSet<Guid>();

            var query = _db.PromotionProducts
                .AsNoTracking()
                .Where(pp => productIds.Contains(pp.ProductId))
                .Join(_db.Promotions.AsNoTracking(),
                    pp => pp.PromotionId,
                    p => p.Id,
                    (pp, p) => new { pp.ProductId, Promotion = p })
                .Where(x => x.Promotion.IsActive
                    && x.Promotion.Priority == priority
                    && x.Promotion.StartsAt <= endsAt
                    && x.Promotion.EndsAt >= startsAt);

            if (promotionIdToExclude.HasValue)
            {
                query = query.Where(x => x.Promotion.Id != promotionIdToExclude.Value);
            }

            var conflictingIds = await query.Select(x => x.ProductId).Distinct().ToListAsync(ct);
            return conflictingIds.ToHashSet();
        }

        // Với mỗi sản phẩm, nếu có NHIỀU Promotion đang active cùng lúc (khác Priority, vì cùng Priority đã bị chặn lúc gán) - chọn Promotion có Priority CAO NHẤT.
        public async Task<Dictionary<Guid, PromotionBadgeDto>> GetActiveDiscountsForProductsAsync(
             List<Guid> productIds, CancellationToken ct = default)
        {
            if (productIds.Count == 0) return new Dictionary<Guid, PromotionBadgeDto>();

            var now = DateTime.UtcNow;

            var rows = await _db.PromotionProducts
                .AsNoTracking()
                .Where(pp => productIds.Contains(pp.ProductId))
                .Join(_db.Promotions.AsNoTracking(),
                    pp => pp.PromotionId,
                    p => p.Id,
                    (pp, p) => new { pp.ProductId, pp.PromotionId, p.Name, p.StartsAt, p.EndsAt, p.IsActive, pp.DiscountPercent, p.Priority })
                .Where(x => x.IsActive && x.StartsAt <= now && x.EndsAt >= now)
                .ToListAsync(ct);

            return rows
                .GroupBy(x => x.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => 
                    {
                        var top = g.OrderByDescending(x => x.Priority).First();
                        return new PromotionBadgeDto
                        {
                            PromotionId = top.PromotionId,
                            Name = top.Name,
                            StartsAt = top.StartsAt,
                            EndsAt = top.EndsAt,
                            IsActive = top.IsActive,
                            DiscountPercent = top.DiscountPercent
                        };
                    });
        }

        public async Task<List<ProductPromotionDetailRow>> GetPromotionsDetailsForProductsAsync(
             List<Guid> productIds, CancellationToken ct = default)
        {
            if (productIds.Count == 0) return new List<ProductPromotionDetailRow>();

            var rows = await _db.PromotionProducts
                .AsNoTracking()
                .Where(pp => productIds.Contains(pp.ProductId))
                .Join(_db.Promotions.AsNoTracking(),
                    pp => pp.PromotionId,
                    p => p.Id,
                    (pp, p) => new ProductPromotionDetailRow(
                        pp.ProductId,
                        p.Id,
                        p.Name,
                        pp.DiscountPercent,
                        p.StartsAt,
                        p.EndsAt,
                        p.IsActive,
                        p.Priority
                    ))
                .ToListAsync(ct);

            return rows;
        }

        public void Add(Promotion promotion) => _db.Promotions.Add(promotion);
        public void Update(Promotion promotion) => _db.Promotions.Update(promotion);
        public void Remove(Promotion promotion) => _db.Promotions.Remove(promotion);
    }
}
