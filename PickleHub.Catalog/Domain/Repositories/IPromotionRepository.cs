using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Enums;

namespace PickleHub.Catalog.Domain.Repositories
{
    public interface IPromotionRepository
    {
        Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<(List<Promotion> Items, int TotalItems)> GetPagedAsync(PromotionStatus? status, int page, int pageSize, CancellationToken ct = default);

        // Chỉ coi là conflict khi overlap ngày VÀ CÙNG Priority - khác Priority thì cho phép
        // chồng lấn tự do (Promotion ưu tiên cao hơn sẽ tự động "đè" lên lúc tính giá
        Task<HashSet<Guid>> GetConflictingProductIdsAsync(
            List<Guid> productIds,
            DateTime startsAt,
            DateTime endsAt,
            int priority,
            Guid? promotionIdToExclude,
            CancellationToken ct = default);

        Task<Dictionary<Guid, PromotionBadgeDto>> GetActiveDiscountsForProductsAsync(
            List<Guid> productIds, CancellationToken ct = default);

        Task<List<ProductPromotionDetailRow>> GetPromotionsDetailsForProductsAsync(
            List<Guid> productIds, CancellationToken ct = default);

        void Add(Promotion promotion);
        void Update(Promotion promotion);
        void Remove(Promotion promotion);
    }

    public record ProductPromotionDetailRow(
        Guid ProductId,
        Guid PromotionId,
        string PromotionName,
        decimal DiscountPercent,
        DateTime StartsAt,
        DateTime EndsAt,
        bool IsActive,
        int Priority
    );
}

