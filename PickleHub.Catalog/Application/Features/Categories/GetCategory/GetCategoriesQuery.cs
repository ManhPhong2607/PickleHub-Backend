using MediatR;
using PickleHub.Catalog.Application.Features.Categories.DTOs;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Application.Features.Categories.GetCategory
{
    public record GetCategoriesQuery(bool Flat = false) : IRequest<List<CategoryTreeDto>>;

    public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, List<CategoryTreeDto>>
    {
        private readonly ICategoryRepository _categoryRepository;
        public GetCategoriesHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }
        public async Task<List<CategoryTreeDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
        {
            var categories = await _categoryRepository.GetAllAsync(ct);

            var dtos = categories.Select(c => new CategoryTreeDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug.Value,
                ParentId = c.ParentId,
                Url = c.Url,
                PublicId = c.PublicId,
                AttributeSchemaJson = c.AttributeSchemaJson,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Children = new List<CategoryTreeDto>()
            }).ToList();

            if (request.Flat)
            {
                return dtos;
            }

            var lookup = dtos.ToDictionary(d => d.Id);
            var roots = new List<CategoryTreeDto>();
            foreach (var d in dtos) 
            {
                if (d.ParentId.HasValue && lookup.TryGetValue(d.ParentId.Value, out var parent))
                {
                    parent.Children.Add(d);
                }
                else
                {
                    roots.Add(d);
                }
            }
            return roots;
        }
    }
}
