using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Application.Features.Products.GetProductVariant
{
    public record GetProductVariantQuery(
     Guid ProductId,
     Guid VariantId) : IRequest<ProductVariantDto>;

    public class GetProductVariantHandler
        : IRequestHandler<GetProductVariantQuery, ProductVariantDto>
    {
        private readonly IProductRepository _productRepository;

        public GetProductVariantHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ProductVariantDto> Handle(
            GetProductVariantQuery request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.ProductId, ct)
                ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

            var variant = product.Variants.FirstOrDefault(v => v.Id == request.VariantId)
                ?? throw new NotFoundException("Không tìm thấy biến thể sản phẩm.");

            // Lấy toàn bộ ảnh riêng của variant (đã sort theo SortOrder).
            // Nếu variant chưa có ảnh riêng, fallback cả list về ảnh chung của Product.
            var ownImages = product.Images
                .Where(i => i.VariantId == variant.Id && !i.IsSizeChart)
                .OrderBy(i => i.SortOrder)
                .ToList();

            var images = ownImages.Count > 0
                ? ownImages
                : product.Images
                    .Where(i => i.VariantId == null && !i.IsSizeChart)
                    .OrderBy(i => i.SortOrder)
                    .ToList();

            var image = images.FirstOrDefault();

            return new ProductVariantDto
            {
                Id = variant.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                Sku = variant.Sku,
                AttributesJson = variant.AttributesJson,
                Price = variant.Price,
                ImageUrl = image?.Url,
                Images = images.Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    PublicId = i.PublicId,
                    Url = i.Url,
                    VariantId = i.VariantId,
                    SortOrder = i.SortOrder,
                    IsSizeChart = i.IsSizeChart
                }).ToList()
            };
        }
    }
}
