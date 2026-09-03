using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Enums;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Products.GetProducts
{
    public record GetProductBySlugQuery(string Slug) : IRequest<ProductDetailDto>;

    public class GetProductBySlugHandler : IRequestHandler<GetProductBySlugQuery, ProductDetailDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IPromotionRepository _promotionRepository;
        private readonly IUnitOfWork _unitOfWork;
        
        public GetProductBySlugHandler(
            IProductRepository productRepository, 
            IPromotionRepository promotionRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _promotionRepository = promotionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProductDetailDto> Handle(GetProductBySlugQuery request, CancellationToken ct)
        {
            var product = await _productRepository.GetBySlugAsync(request.Slug, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            if (product.Status != ProductStatus.Active)
                throw new NotFoundException("Sản phẩm không tồn tại.");

            product.IncreaseViewCount();
            _productRepository.Update(product);
            await _productRepository.IncrementDailyViewAsync(product.Id, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            var discounts = await _promotionRepository.GetActiveDiscountsForProductsAsync(
                new List<Guid> { product.Id }, ct);
            discounts.TryGetValue(product.Id, out var activePromotion);

            return product.MapToDetailDto(activePromotion);
        }
    }
}
