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
    public record GetProductsQuery(
        string? Keyword,
        Guid? CategoryId,
        Guid? BrandId,
        decimal? MinPrice,
        decimal? MaxPrice,
        SortBy SortBy = SortBy.Newest,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<ProductListDto>>;

    public class GetProductsHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductListDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IPromotionRepository _promotionRepository;

        public GetProductsHandler(IProductRepository productRepository, IPromotionRepository promotionRepository)
        {
            _productRepository = productRepository;
            _promotionRepository = promotionRepository;
        }
        public async Task<PagedResult<ProductListDto>> Handle(GetProductsQuery request, CancellationToken ct)
        {
            var (items, totalItems) = await _productRepository.GetPagedAsync(
                request.Keyword, request.CategoryId, request.BrandId,
                request.MinPrice, request.MaxPrice,
                request.SortBy, request.Page, request.PageSize,
                ct);

            // 1 query duy nhất lấy % giảm đang active cho TOÀN BỘ sản phẩm trong trang này -
            // không lặp N lần theo từng sản phẩm (tránh N+1).
            var productIds = items.Select(p => p.Id).ToList();
            var discounts = await _promotionRepository.GetActiveDiscountsForProductsAsync(productIds, ct);

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
                        ThumbnailUrl = p.ResolveThumbnailUrl(),
                        Brand = p.Brand is null ? null : new BrandDto
                        {
                            Id = p.Brand.Id,
                            Name = p.Brand.Name
                        },
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
                TotalItems = totalItems,
            };
        }
    }
}
