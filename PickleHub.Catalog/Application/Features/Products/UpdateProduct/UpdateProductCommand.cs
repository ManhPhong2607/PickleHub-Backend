using MassTransit;
using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Enums;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Events.Catalog;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Application.Features.Products.UpdateProduct
{
    public record UpdateProductCommand(
       Guid Id,
       string Name,
       string Description,
       Guid CategoryId,
       Guid BrandId,
       string? SpecsJson = null,
       decimal? Price = null,
       decimal? BasePrice = null,
       ProductStatus? Status = null
    ) : IRequest<ProductDetailDto>;

    public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductDetailDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICurrentUserService _currentUser;
        public UpdateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint,  ICurrentUserService currentUser)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _currentUser = currentUser;
        }

        public async Task<ProductDetailDto> Handle(UpdateProductCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Sản phẩm không tồn tại.");

            // Chỉ tự sinh lại slug khi sản phẩm còn Draft (chưa từng public).
            // Đã Active/Hidden -> giữ nguyên slug, tránh vỡ link đã chia sẻ / đã được Google index.
            var slug = product.Status == ProductStatus.Draft
                ? await GenerateUniqueSlugAsync(request.Name, request.Id, ct)
                : product.Slug;

            var newPrice = request.Price ?? request.BasePrice;

            // Nếu sản phẩm chỉ có đúng 1 biến thể và có truyền giá mới, cập nhật giá cho biến thể đó
            if (newPrice.HasValue && newPrice.Value > 0 && product.Variants.Count == 1)
            {
                var singleVariant = product.Variants.First();
                product.UpdateVariant(singleVariant.Id, singleVariant.Sku, singleVariant.AttributesJson, newPrice.Value);
            }

            var effectiveBasePrice = product.Variants.Any() 
                ? product.Variants.Min(v => v.Price) 
                : (newPrice ?? product.BasePrice);

            product.Update(
                request.Name,
                slug,
                request.Description,
                request.CategoryId,
                request.BrandId,
                request.SpecsJson ?? "{}"
            );

            if (request.Status.HasValue)
            {
                product.SetStatus(request.Status.Value);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new ProductUpdatedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UpdatedByUserId = _currentUser.UserId,
                UpdatedByEmail = _currentUser.Email ?? string.Empty,
                OccurredAt = DateTime.UtcNow
            }, ct);

            var minPrice = product.Variants.Any() ? product.Variants.Min(v => v.Price) : product.BasePrice;
            var maxPrice = product.Variants.Any() ? product.Variants.Max(v => v.Price) : product.BasePrice;

            return new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug.Value,
                Description = product.Description,
                BasePrice = minPrice,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                EffectivePrice = minPrice,
                EffectiveMinPrice = minPrice,
                EffectiveMaxPrice = maxPrice,
                Status = product.Status.ToString(),
                SpecsJson = product.SpecsJson,
                Variants = product.Variants.Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    Sku = v.Sku,
                    AttributesJson = v.AttributesJson,
                    Price = v.Price
                }).ToList()
            };
        }

        private async Task<Slug> GenerateUniqueSlugAsync(string name, Guid? excludeId, CancellationToken ct)
        {
            var baseSlug = Slug.Create(name);
            var candidate = baseSlug;
            var counter = 1;

            while (await _productRepository.ExistsBySlugAsync(candidate.Value, excludeId, ct))
                candidate = baseSlug.AppendSuffix(counter++);

            return candidate;
        }
    }
}
