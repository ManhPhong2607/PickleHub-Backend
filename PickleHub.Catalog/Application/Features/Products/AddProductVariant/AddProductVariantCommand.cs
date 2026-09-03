using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Application.Features.Products.AddProductVarriant
{
    public record AddProductVariantCommand(
       Guid ProductId,
       string Sku,
       string AttributesJson,
       decimal Price
    ) : IRequest<ProductVariantDto>;

    public class AddProductVariantHandler : IRequestHandler<AddProductVariantCommand, ProductVariantDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddProductVariantHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProductVariantDto> Handle(AddProductVariantCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.ProductId, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            var variant = product.AddVariant(request.Sku, request.AttributesJson, request.Price);


            await _unitOfWork.SaveChangesAsync(ct);

            return new ProductVariantDto
            {
                Id = variant.Id,
                Sku = variant.Sku,
                AttributesJson = variant.AttributesJson,
                Price = variant.Price
            };
        }
    }
}
