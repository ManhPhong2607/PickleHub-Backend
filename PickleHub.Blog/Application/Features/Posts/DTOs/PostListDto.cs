namespace PickleHub.Blog.Application.Features.Posts.DTOs
{
    public class PostListDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Summary { get; set; }
        public string? CoverImageUrl { get; set; }
        public string CategoryName { get; set; } = null!;
        public string CategorySlug { get; set; } = null!;
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }
    }
}
