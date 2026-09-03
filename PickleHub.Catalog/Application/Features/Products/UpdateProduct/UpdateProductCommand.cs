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
       string? SpecsJson
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

            product.Update(
                request.Name,
                slug,
                request.Description,
                request.CategoryId,
                request.BrandId,
                request.SpecsJson ?? "{}"
            );

            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new ProductUpdatedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UpdatedByUserId = _currentUser.UserId,
                UpdatedByEmail = _currentUser.Email ?? string.Empty,
                OccurredAt = DateTime.UtcNow
            }, ct);

            return new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug.Value,
                Description = product.Description,
                BasePrice = product.BasePrice,
                Status = product.Status.ToString(),
                SpecsJson = product.SpecsJson,
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
