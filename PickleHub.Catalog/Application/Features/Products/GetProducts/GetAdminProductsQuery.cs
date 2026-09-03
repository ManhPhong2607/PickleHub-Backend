using MediatR;
using PickleHub.Catalog.Application.Features.Brands.DTOs;
using PickleHub.Catalog.Application.Features.Categories.DTOs;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Enums;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.DTOs;

namespace PickleHub.Catalog.Application.Features.Products.GetProducts
{
    public record GetAdminProductsQuery(
        string? Keyword,
        Guid? CategoryId,
        Guid? BrandId,
        decimal? MinPrice,
        decimal? MaxPrice,
        ProductStatus? Status,   
        SortBy SortBy = SortBy.Newest,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<ProductListDto>>;

    public class GetAdminProductsHandler : IRequestHandler<GetAdminProductsQuery, PagedResult<ProductListDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IPromotionRepository _promotionRepository;

        public GetAdminProductsHandler(IProductRepository productRepository, IPromotionRepository promotionRepository)
        {
            _productRepository = productRepository;
            _promotionRepository = promotionRepository;
        }

        public async Task<PagedResult<ProductListDto>> Handle(
            GetAdminProductsQuery request, CancellationToken cancellationToken)
        {
            var (items, totalItems) = await _productRepository.GetAdminPagedAsync(
                request.Keyword, request.CategoryId, request.BrandId,
                request.MinPrice, request.MaxPrice,
                request.Status,
                request.SortBy, request.Page, request.PageSize,
                cancellationToken);

            // 1 query duy nhất lấy toàn bộ chi tiết khuyến mãi cho toàn bộ sản phẩm trong trang này.
            var productIds = items.Select(p => p.Id).ToList();
            var discounts = await _promotionRepository.GetActiveDiscountsForProductsAsync(productIds, cancellationToken);
            var promoRows = await _promotionRepository.GetPromotionsDetailsForProductsAsync(productIds, cancellationToken);
            var now = DateTime.UtcNow;

            var promoGrouped = promoRows
                .GroupBy(x => x.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(r =>
                    {
                        string status;
                        if (!r.IsActive || now > r.EndsAt)
                            status = "Expired";
                        else if (now < r.StartsAt)
                            status = "Scheduled";
                        else
                            status = "Active";

                        return new ProductPromotionSummaryDto
                        {
                            PromotionId = r.PromotionId,
                            PromotionName = r.PromotionName,
                            DiscountPercent = r.DiscountPercent,
                            StartsAt = r.StartsAt,
                            EndsAt = r.EndsAt,
                            IsActive = r.IsActive,
                            Priority = r.Priority,
                            Status = status
                        };
                    }).OrderByDescending(x => x.Priority).ToList()
                );

            return new PagedResult<ProductListDto>
            {
                Items = items.Select(p =>
                {
                    discounts.TryGetValue(p.Id, out var activePromotion);
                    var isOnSale = activePromotion != null && activePromotion.DiscountPercent > 0;
                    var discountPercent = isOnSale ? activePromotion!.DiscountPercent : 0m;
                    var minPrice = p.Variants.Any() ? p.Variants.Min(v => v.Price) : p.BasePrice;
                    var maxPrice = p.Variants.Any() ? p.Variants.Max(v => v.Price) : p.BasePrice;
                    var effectiveMinPrice = isOnSale ? Math.Round(minPrice * (1 - discountPercent / 100m), 0) : minPrice;
                    var effectiveMaxPrice = isOnSale ? Math.Round(maxPrice * (1 - discountPercent / 100m), 0) : maxPrice;

                    return new ProductListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug.Value,
                        BasePrice = minPrice,
                        MinPrice = minPrice,
                        MaxPrice = maxPrice,
                        EffectivePrice = effectiveMinPrice,
                        EffectiveMinPrice = effectiveMinPrice,
                        EffectiveMaxPrice = effectiveMaxPrice,
                        IsOnSale = isOnSale,
                        SalePercent = isOnSale ? activePromotion.DiscountPercent : null,
                        ActivePromotion = activePromotion,
                        SoldCount = p.SoldCount,
                        ViewCount = p.ViewCount,
                        Status = p.Status.ToString(),
                        ThumbnailUrl = p.ResolveThumbnailUrl(),
                        Brand = p.Brand is null ? null : new BrandDto { Id = p.Brand.Id, Name = p.Brand.Name },
                        Category = p.Category is null ? null : new CategorySummaryDto
                        {
                            Id = p.Category.Id,
                            Name = p.Category.Name,
                            Slug = p.Category.Slug.Value
                        },
                        Variants = p.Variants.Select(v => new ProductVariantDto
                        {
                            Id = v.Id,
                            ProductId = v.ProductId,
                            Sku = v.Sku,
                            AttributesJson = v.AttributesJson,
                            Price = v.Price,
                            EffectivePrice = isOnSale
                                ? Math.Round(v.Price * (1 - discountPercent / 100m), 0)
                                : v.Price
                        }).ToList()
                    };
                }).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems
            };
        }
    }
}
