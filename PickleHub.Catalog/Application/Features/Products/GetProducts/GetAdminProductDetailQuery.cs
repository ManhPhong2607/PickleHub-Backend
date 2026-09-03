using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Application.Features.Products.GetProducts
{
    public record GetAdminProductDetailQuery(string Value) : IRequest<ProductDetailDto?>;

    public class GetAdminProductDetailHandler : IRequestHandler<GetAdminProductDetailQuery, ProductDetailDto?>
    {
        private readonly IProductRepository _productRepository;
        private readonly IPromotionRepository _promotionRepository;

        public GetAdminProductDetailHandler(IProductRepository productRepository, IPromotionRepository promotionRepository)
        {
            _productRepository = productRepository;
            _promotionRepository = promotionRepository;
        }

        public async Task<ProductDetailDto?> Handle(GetAdminProductDetailQuery request, CancellationToken ct)
        {
            Product? product = Guid.TryParse(request.Value, out var id)
                  ? await _productRepository.GetByIdWithDetailAsync(id, ct)
                  : await _productRepository.GetBySlugAsync(request.Value, ct);

            if (product == null) 
                return null;

            var discounts = await _promotionRepository.GetActiveDiscountsForProductsAsync(
                new List<Guid> { product.Id }, ct);
            discounts.TryGetValue(product.Id, out var activePromotion);

            var dto = product.MapToDetailDto(activePromotion);
            
            return dto;
        }
    }
}
