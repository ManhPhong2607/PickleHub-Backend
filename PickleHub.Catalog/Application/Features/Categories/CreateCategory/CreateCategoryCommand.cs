using PickleHub.Common.Interfaces;
using MediatR;
using PickleHub.Catalog.Application.Features.Categories.DTOs;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Application.Features.Categories.CreateCategory
{
    public record CreateCategoryCommand(string Name, Guid? ParentId) : IRequest<CategoryTreeDto>;

    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CategoryTreeDto>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        public CreateCategoryHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<CategoryTreeDto> Handle(CreateCategoryCommand request, CancellationToken ct)
        {
            var slug = await GenerateUniqueSlugAsync(request.Name,null, ct);
            var category = Category.Create(request.Name, slug, request.ParentId);

            _categoryRepository.Add(category);
            await _unitOfWork.SaveChangesAsync(ct);
            return new CategoryTreeDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug.Value,
                ParentId = category.ParentId,
                AttributeSchemaJson = category.AttributeSchemaJson,
            };
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
