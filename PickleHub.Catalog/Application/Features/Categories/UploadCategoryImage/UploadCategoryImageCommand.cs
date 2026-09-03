using MediatR;
using Microsoft.AspNetCore.Http;
using PickleHub.Catalog.Application.Features.Categories.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Categories.UploadCategoryImage
{
    public record UploadCategoryImageCommand(Guid CategoryId, IFormFile File) : IRequest<CategoryTreeDto>;

    public class UploadCategoryImageHandler : IRequestHandler<UploadCategoryImageCommand, CategoryTreeDto>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storage;

        public UploadCategoryImageHandler(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            IStorageService storage)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _storage = storage;
        }

        public async Task<CategoryTreeDto> Handle(UploadCategoryImageCommand request, CancellationToken ct)
        {
            if (request.File == null || request.File.Length == 0)
                throw new DomainException("File ảnh không hợp lệ.");

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct)
                ?? throw new NotFoundException("Danh mục không tồn tại.");

            await using var stream = request.File.OpenReadStream();

            var uploadResult = await _storage.UploadAsync(
                fileStream: stream,
                fileName: request.File.FileName,
                folder: "categories",
                resourceType: "image",
                ct: ct);

            var oldPublicId = category.PublicId;

            category.SetImage(uploadResult.SecureUrl, uploadResult.PublicId);
            _categoryRepository.Update(category);

            await _unitOfWork.SaveChangesAsync(ct);

            // Clean up old image on Cloudinary if replaced
            if (!string.IsNullOrWhiteSpace(oldPublicId) && oldPublicId != uploadResult.PublicId)
            {
                try
                {
                    await _storage.DeleteAsync(oldPublicId);
                }
                catch
                {
                    // Log or ignore non-critical delete failure
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
