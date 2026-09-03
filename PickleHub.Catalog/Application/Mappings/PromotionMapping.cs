
using PickleHub.Catalog.Application.Features.Promotions.DTOs;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Application.Mappings
{
    public static class PromotionMapping
    {
        public static async Task<PromotionDto> MapToDtoAsync(this Promotion promotion, IProductRepository productRepository, CancellationToken ct)
        {
            var productIds = promotion.Items.Select(i => i.ProductId).ToList();

            // 1 query duy nhất lấy tên/ảnh cho toàn bộ sản phẩm trong Promotion, không lặp N+1.
            var products = productIds.Count == 0
                ? new List<Product>()
                : await productRepository.GetByIdsAsync(productIds, ct);

            var productLookup = products.ToDictionary(p => p.Id);

            return new PromotionDto
            {
                Id = promotion.Id,
                Name = promotion.Name,
                Description = promotion.Description,
                StartsAt = promotion.StartsAt,
                EndsAt = promotion.EndsAt,
                IsActive = promotion.IsActive,
                Priority = promotion.Priority,
                IsCurrentlyRunning = promotion.IsCurrentlyRunning,
                Items = promotion.Items.Select(i =>
                {
                    productLookup.TryGetValue(i.ProductId, out var product);
                    return new PromotionItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = product?.Name ?? "(Sản phẩm không còn tồn tại)",
                        ThumbnailUrl = product?.ResolveThumbnailUrl(),
                        DiscountPercent = i.DiscountPercent
                    };
                }).ToList()
            };
        }
    }
}
