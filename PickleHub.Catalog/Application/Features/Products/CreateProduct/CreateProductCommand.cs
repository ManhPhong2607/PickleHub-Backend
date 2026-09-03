using CloudinaryDotNet.Actions;
using MassTransit;
using MassTransit.DependencyInjection;
using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Catalog.Infrastructure.Persistence.Repositories;
using PickleHub.Common.Events.Catalog;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Application.Features.Products.CreateProduct
{
    public record CreateProductCommand(
        string Name,
        string Description,
        Guid CategoryId,
        Guid BrandId,
        string? SpecsJson
    ) : IRequest<ProductDetailDto>;

    public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDetailDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICurrentUserService _currentUser;

        public CreateProductHandler(
            IProductRepository productRepository, 
            IUnitOfWork unitOfWork,
            ICategoryRepository categoryRepository,
            IBrandRepository brandRepository,
            IPublishEndpoint publishEndpoint,
            ICurrentUserService currentUser)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _publishEndpoint = publishEndpoint;
            _currentUser = currentUser;
        }
        public async Task<ProductDetailDto> Handle(CreateProductCommand request, CancellationToken ct)
        {
            var slug = await GenerateUniqueSlugAsync(request.Name,null, ct);
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct)
                ?? throw new NotFoundException("Danh mục sản phẩm không tồn tại.");

            var brand = await _brandRepository.GetByIdAsync(request.BrandId, ct)
                ?? throw new NotFoundException("Thương hiệu không tồn tại.");
            var product = Product.Create(
                request.Name,
                slug,
                request.Description,
                request.CategoryId,
                request.BrandId,
                request.SpecsJson ?? "{}"
            );
            _productRepository.Add(product);
            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new ProductCreatedEvent
            {
                ProductId = product.Id,
                ProductName = product.Name,
                CategoryName = category.Name,
                BrandName = brand.Name,
                CreatedByUserId = _currentUser.UserId,
                CreatedByEmail = _currentUser.Email ?? string.Empty,
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

            while(await _productRepository.ExistsBySlugAsync(candidate.Value, excludeId, ct))
            {
                candidate = baseSlug.AppendSuffix(counter++);
            }
            return candidate;
        }

    }
}
