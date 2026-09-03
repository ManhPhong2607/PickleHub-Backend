using PickleHub.Blog.Application.Features.Posts.DTOs;
using PickleHub.Blog.Domain.Entities;

namespace PickleHub.Blog.Application.Mappings
{
    public static class PostMapping
    {
        public static PostDetailDto MapToDetailDto(this Post post) => new()
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug.Value,
            Summary = post.Summary,
            Content = post.Content,
            CoverImageUrl = post.CoverImageUrl,
            CategoryId = post.CategoryId,
            CategoryName = post.Category?.Name ?? string.Empty,
            Status = post.Status.ToString(),
            PublishedAt = post.PublishedAt,
            AuthorId = post.AuthorId,
            ViewCount = post.ViewCount,
            SeoTitle = post.SeoTitle,
            SeoDescription = post.SeoDescription,
            RelatedProductIds = post.RelatedProductIds
        };

        public static PostListDto MapToListDto(this Post post) => new()
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug.Value,
            Summary = post.Summary,
            CoverImageUrl = post.CoverImageUrl,
            CategoryName = post.Category?.Name ?? string.Empty,
            CategorySlug = post.Category?.Slug.Value ?? string.Empty,
            PublishedAt = post.PublishedAt,
            ViewCount = post.ViewCount
        };

        public static AdminPostListDto MapToAdminListDto(this Post post) => new()
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug.Value,
            Status = post.Status.ToString(),
            CategoryName = post.Category?.Name ?? string.Empty,
            CoverImageUrl = post.CoverImageUrl,
            PublishedAt = post.PublishedAt,
            CreatedAt = post.CreatedAt,
            ViewCount = post.ViewCount
        };
    }
}
