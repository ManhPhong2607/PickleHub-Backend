using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Catalog.Application.Features.Categories.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Application.Features.Categories.UpdateCategory
{
    public record UpdateCategoryCommand(Guid Id, string Name, Guid? ParentId) : IRequest<CategoryTreeDto>;

    public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryTreeDto>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCategoryHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryTreeDto> Handle(UpdateCategoryCommand request, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Danh mục không tồn tại.");

            if (request.ParentId.HasValue)
                await EnsureNoCycleAsync(request.Id, request.ParentId.Value, ct);

            var slug = await GenerateUniqueSlugAsync(request.Name, request.Id, ct);
            category.Update(request.Name, slug, request.ParentId);

            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync(ct);

            return new CategoryTreeDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug.Value,
                ParentId = category.ParentId,
                AttributeSchemaJson = category.AttributeSchemaJson
            };
        }

        // Chặn set ParentId tạo thành vòng lặp: chính nó, hoặc 1 trong các category con/cháu của nó.
        private async Task EnsureNoCycleAsync(Guid categoryId, Guid newParentId, CancellationToken ct)
        {
            if (newParentId == categoryId)
                throw new DomainException("Danh mục không thể là danh mục cha của chính nó.");

            var allCategories = await _categoryRepository.GetAllAsync(ct);
            var parentLookup = allCategories.ToDictionary(c => c.Id, c => c.ParentId);

            // Đi ngược từ newParentId lên tới gốc, nếu gặp lại categoryId -> có vòng lặp.
            var current = newParentId;
            var visited = new HashSet<Guid>();
            while (parentLookup.TryGetValue(current, out var parent) && parent.HasValue)
            {
                if (!visited.Add(current))
                    break; //  tránh loop vô hạn nếu data cây đã lỡ hỏng từ trước

                if (parent.Value == categoryId)
                    throw new DomainException("Không thể chọn danh mục con/cháu làm danh mục cha (sẽ tạo vòng lặp).");

                current = parent.Value;
            }
        }

        private async Task<Slug> GenerateUniqueSlugAsync(string name, Guid? excludeId, CancellationToken ct)
        {
            var baseSlug = Slug.Create(name);
            var candidate = baseSlug;
            var counter = 1;

            while (await _categoryRepository.ExistsBySlugAsync(candidate.Value, excludeId, ct))
                candidate = baseSlug.AppendSuffix(counter++);

            return candidate;
        }
    }
}
