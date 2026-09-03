using MediatR;
using PickleHub.Blog.Application.Features.Categories.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Entities;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Blog.Application.Features.Categories.UpdateCategory
{
    public record UpdateCategoryCommand(Guid Id, string Name, string? Description, int DisplayOrder) : IRequest<CategoryDto>;

    public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
    {
        private readonly IContentCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCategoryHandler(IContentCategoryRepository categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Không tìm thấy category.");

            var slug = category.Slug;

            if (!Slug.Create(request.Name).Equals(category.Slug))
            {
                var hasPosts = await _categoryRepository.HasPostsAsync(
                    request.Id, ct);

                if (!hasPosts)
                {
                    slug = await GenerateUniqueSlugAsync(
                        request.Name,
                        request.Id,
                        ct);
                }
            }

            category.Update(request.Name, slug, request.Description, request.DisplayOrder);
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
