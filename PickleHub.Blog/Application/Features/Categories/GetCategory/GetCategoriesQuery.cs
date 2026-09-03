using MediatR;
using PickleHub.Blog.Application.Features.Categories.DTOs;
using PickleHub.Blog.Application.Mappings;
using PickleHub.Blog.Domain.Repositories;

namespace PickleHub.Blog.Application.Features.Categories.GetCategory
{
    public record GetCategoriesQuery : IRequest<List<CategoryDto>>;

    public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
    {
        private readonly IContentCategoryRepository _categoryRepository;

        public GetCategoriesHandler(IContentCategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
        {
            var categories = await _categoryRepository.GetAllAsync(ct);
            return categories.Select(c => c.MapToDto()).ToList();

        }
    }
}


