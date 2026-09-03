namespace PickleHub.Blog.Application.Features.Posts.DTOs
{
    public class AdjacentPostDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }
}
