using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Application.Features.Products.UpdateProductVariant
{
    public record UpdateProductVariantCommand(
       Guid ProductId,
       Guid VariantId,
       string Sku,
       string AttributesJson,
       decimal Price
    ) : IRequest<ProductVariantDto>;

    public class UpdateProductVariantHandler : IRequestHandler<UpdateProductVariantCommand, ProductVariantDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateProductVariantHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProductVariantDto> Handle(UpdateProductVariantCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.ProductId, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            product.UpdateVariant(request.VariantId, request.Sku, request.AttributesJson, request.Price);


            await _unitOfWork.SaveChangesAsync(ct);

            var variant = product.Variants.First(v => v.Id == request.VariantId);
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
