
using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Application.Features.Products.GetProducts
{
    public record GetProductDetailInternalQuery(Guid ProductId) : IRequest<ProductDetailDto>;
    public class GetProductDetailInternalHandler : IRequestHandler<GetProductDetailInternalQuery, ProductDetailDto?>
    {
        private readonly IProductRepository _productRepository;
        private readonly IPromotionRepository _promotionRepository;

        public GetProductDetailInternalHandler(
            IProductRepository productRepository,
            IPromotionRepository promotionRepository)
        {
            _productRepository = productRepository;
            _promotionRepository = promotionRepository;
        }

        public async Task<ProductDetailDto?> Handle(GetProductDetailInternalQuery request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.ProductId, ct);
            // Trả null thay vì throw NotFoundException - endpoint nội bộ trả 404 đơn giản,
            // để service gọi sang (CartOrder) tự quyết định xử lý null thế nào.
            if (product is null) return null;

            var discounts = await _promotionRepository.GetActiveDiscountsForProductsAsync(
                new List<Guid> { product.Id }, ct);
            discounts.TryGetValue(product.Id, out var activePromotion);

            return product.MapToDetailDto(activePromotion);
        }
    }
}
