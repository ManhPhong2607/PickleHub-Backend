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

            // 1 query duy nhất lấy % giảm đang active cho toàn bộ sản phẩm trong trang này.
            var productIds = items.Select(p => p.Id).ToList();
            var discounts = await _promotionRepository.GetActiveDiscountsForProductsAsync(productIds, cancellationToken);

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
                        Status = p.Status.ToString(),
                        ThumbnailUrl = p.ResolveThumbnailUrl(),
                        Brand = p.Brand is null ? null : new BrandDto { Id = p.Brand.Id, Name = p.Brand.Name },
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
                TotalItems = totalItems
            };
        }
    }
}
