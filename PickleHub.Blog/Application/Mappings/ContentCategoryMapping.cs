using PickleHub.Blog.Application.Features.Categories.DTOs;
using PickleHub.Blog.Domain.Entities;

namespace PickleHub.Blog.Application.Mappings
{
    public static class ContentCategoryMapping
    {
        public static CategoryDto MapToDto(this ContentCategory category) => new()
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug.Value,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder
        };
    }
}
