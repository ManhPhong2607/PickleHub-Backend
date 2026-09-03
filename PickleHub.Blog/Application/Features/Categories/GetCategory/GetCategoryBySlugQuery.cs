using MediatR;
using PickleHub.Blog.Application.Features.Categories.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Blog.Application.Features.Categories.GetCategory
{
    // Public — lấy 1 category theo slug (dùng cho trang danh mục, breadcrumb, SEO...)
    public record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryDto>;

    public class GetCategoryHandler : IRequestHandler<GetCategoryBySlugQuery, CategoryDto>
    {
        private readonly IContentCategoryRepository _categoryRepository;

        public GetCategoryHandler(IContentCategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<CategoryDto> Handle(GetCategoryBySlugQuery request, CancellationToken ct)
        {
            var category = await _categoryRepository.GetBySlugAsync(request.Slug, ct)
                ?? throw new NotFoundException("Không tìm thấy category.");

            return category.MapToDto();
        }
    }
}
