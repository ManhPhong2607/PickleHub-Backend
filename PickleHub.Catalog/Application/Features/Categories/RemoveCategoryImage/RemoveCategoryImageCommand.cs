using MediatR;
using PickleHub.Catalog.Application.Features.Categories.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Categories.RemoveCategoryImage
{
    public record RemoveCategoryImageCommand(Guid CategoryId) : IRequest<CategoryTreeDto>;

    public class RemoveCategoryImageHandler : IRequestHandler<RemoveCategoryImageCommand, CategoryTreeDto>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storage;

        public RemoveCategoryImageHandler(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            IStorageService storage)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _storage = storage;
        }

        public async Task<CategoryTreeDto> Handle(RemoveCategoryImageCommand request, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct)
                ?? throw new NotFoundException("Danh mục không tồn tại.");

            var publicId = category.PublicId;

            category.RemoveImage();

            await _unitOfWork.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(publicId))
            {
                try
                {
                    await _storage.DeleteAsync(publicId);
                }
                catch
                {
                    // Ignore non-critical delete failure
                }
            }

            return new CategoryTreeDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug.Value,
                ParentId = category.ParentId,
                Url = category.Url,
                PublicId = category.PublicId,
                AttributeSchemaJson = category.AttributeSchemaJson,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
            };
        }
    }
}
