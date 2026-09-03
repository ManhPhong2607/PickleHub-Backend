using PickleHub.Blog.Application.Common.Interfaces;

namespace PickleHub.Blog.Application.Features.Posts.DTOs
{
    public class PostDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Summary { get; set; }
        public string Content { get; set; } = null!;
        public string? CoverImageUrl { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? PublishedAt { get; set; }
        public Guid AuthorId { get; set; }
        public int ViewCount { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }

        // ID thô — dùng cho form Admin edit (multi-select sản phẩm)
        public List<Guid>? RelatedProductIds { get; set; }

        // Data đã resolve từ Catalog — dùng để hiển thị card sản phẩm ở trang public
        public List<ProductSummary>? RelatedProducts { get; set; }

        public AdjacentPostDto? PreviousPost { get; set; }
        public AdjacentPostDto? NextPost { get; set; }
    }
}
