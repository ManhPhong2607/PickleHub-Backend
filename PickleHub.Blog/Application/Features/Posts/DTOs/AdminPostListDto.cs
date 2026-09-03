namespace PickleHub.Blog.Application.Features.Posts.DTOs
{
    public class AdminPostListDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string? CoverImageUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ViewCount { get; set; }
    }
}
