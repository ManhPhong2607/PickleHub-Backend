using MediatR;
using PickleHub.Catalog.Application.Features.Brands.DTOs;
using PickleHub.Catalog.Application.Features.Categories.DTOs;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Application.Features.Products.GetProducts
{
    public record GetRelatedProductsQuery(Guid ProductId, int Limit = 8) : IRequest<List<ProductListDto>>;

    public class GetRelatedProductsHandler : IRequestHandler<GetRelatedProductsQuery, List<ProductListDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IPromotionRepository _promotionRepository;

        public GetRelatedProductsHandler(IProductRepository productRepository, IPromotionRepository promotionRepository)
        {
            _productRepository = productRepository;
            _promotionRepository = promotionRepository;
        }

        public async Task<List<ProductListDto>> Handle(GetRelatedProductsQuery request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            var related = await _productRepository.GetRelatedAsync(
                product.Id, product.CategoryId, request.Limit, ct);

            // 1 query duy nhất lấy % giảm đang active cho toàn bộ sản phẩm liên quan.
            var productIds = related.Select(p => p.Id).ToList();
            var discounts = await _promotionRepository.GetActiveDiscountsForProductsAsync(productIds, ct);

            return related.Select(p =>
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
            }).ToList();
        }
    }
}
