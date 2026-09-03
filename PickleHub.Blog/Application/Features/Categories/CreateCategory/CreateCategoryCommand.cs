using MediatR;
using PickleHub.Blog.Application.Features.Categories.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Entities;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Interfaces;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Blog.Application.Features.Categories.CreateCategory
{
    public record CreateCategoryCommand(string Name, string? Description, int DisplayOrder) : IRequest<CategoryDto>;
    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
    {
        private readonly IContentCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCategoryHandler(IContentCategoryRepository categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
        {
            var slug = await GenerateUniqueSlugAsync(request.Name, null, ct);
            var category = ContentCategory.Create(request.Name, slug, request.Description, request.DisplayOrder);
            _categoryRepository.Add(category);
            await _unitOfWork.SaveChangesAsync(ct);
            return category.MapToDto();
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
