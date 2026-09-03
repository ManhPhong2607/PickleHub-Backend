using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Application.Features.Products.GetProductVariant
{
    public record GetProductVariantsQuery(Guid ProductId)
     : IRequest<List<ProductVariantDto>>;

    public class GetProductVariantsHandler
        : IRequestHandler<GetProductVariantsQuery, List<ProductVariantDto>>
    {
        private readonly IProductRepository _productRepository;

        public GetProductVariantsHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<ProductVariantDto>> Handle(
            GetProductVariantsQuery request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdWithDetailAsync(request.ProductId, ct)
                ?? throw new NotFoundException("Không tìm thấy sản phẩm.");

            return product.Variants.OrderBy(v => v.CreatedAt)
                .Select(variant =>
            {
                var ownImages = product.Images
                    .Where(i => i.VariantId == variant.Id && !i.IsSizeChart)
                    .OrderBy(i => i.SortOrder)
                    .ToList();

                // Variant chưa có ảnh riêng -> fallback cả list về ảnh chung của Product,
                // tránh FE nhận về gallery trống dù sản phẩm thực ra có ảnh dùng được.
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
            }).ToList();
        }
    }
}
