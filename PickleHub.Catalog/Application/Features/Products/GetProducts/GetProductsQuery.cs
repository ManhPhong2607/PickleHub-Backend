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
                    var effectivePrice = isOnSale
                        ? Math.Round(p.BasePrice * (1 - activePromotion.DiscountPercent / 100m), 0)
                        : p.BasePrice;

                    return new ProductListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug.Value,
                        BasePrice = p.BasePrice,
                        EffectivePrice = effectivePrice,
                        IsOnSale = isOnSale,
                        SalePercent = isOnSale ? activePromotion.DiscountPercent : null,
                        ActivePromotion = activePromotion,
                        SoldCount = p.SoldCount,
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
                        }
                    };
                }).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems,
            };
        }
    }
}
