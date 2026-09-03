using PickleHub.Blog.Domain.Enums;
using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Blog.Domain.Entities
{
    public class Post : BaseEntity
    {
        public string Title { get; private set; } = null!;
        public Slug Slug { get; private set; } = null!;
        public string? Summary { get; private set; }
        public string Content { get; private set; } = null!;
        public string? CoverImageUrl { get; private set; }
        public string? CoverImagePublicId { get; private set; }
        public Guid CategoryId { get; private set; }
        public ContentCategory? Category { get; private set; }
        public PostStatus Status { get; private set; } = PostStatus.Draft;
        public DateTime? PublishedAt { get; private set; }
        public Guid AuthorId { get; private set; }
        public int ViewCount { get; private set; } 
        public string? SeoTitle { get; private set; }
        public string? SeoDescription { get; private set; }
        public List<Guid>? RelatedProductIds { get; private set; }

        public bool CanBeDeleted => Status != PostStatus.Published;
        private Post() { }

        public static Post Create(
            string title,
            Slug slug,
            string content,
            Guid categoryId,
            Guid authorId,
            string? summary = null,
            string? seoTitle = null,
            string? seoDescription = null
        )
        {
            if(string.IsNullOrWhiteSpace(title))
                throw new DomainException("Tiêu đề bài viết không được để trống.");
            if(string.IsNullOrWhiteSpace(content))
                throw new DomainException("Nội dung bài viết không được để trống.");

            return new Post
            {
                Title = title.Trim(),
                Slug = slug,
                Summary = summary,
                Content = content,
                CategoryId = categoryId,
                AuthorId = authorId,
                Status = PostStatus.Draft,
                ViewCount = 0,
                SeoTitle = seoTitle,
                SeoDescription = seoDescription
            };
        }

        public void UpdateContent(
            string title,
            Slug slug,
            string content,
            Guid categoryId,
            string? summary,
            string? seoTitle,
            string? seoDescription
        )
        {
            if(string.IsNullOrWhiteSpace(title))
                throw new DomainException("Tiêu đề bài viết không được để trống.");
            if (string.IsNullOrWhiteSpace(content))
                throw new DomainException("Nội dung bài viết không được để trống.");

            Title = title.Trim();
            Slug = slug;
            Content = content;
            CategoryId = categoryId;
            Summary = summary;
            SeoTitle = seoTitle;
            SeoDescription = seoDescription;
            SetUpdated();
        }

        public void SetCoverImage(string url, string publicId)
        {
            CoverImageUrl = url;
            CoverImagePublicId = publicId;
            SetUpdated();
        }

        public void SetRelatedProducts(List<Guid>? productIds)
        {
            RelatedProductIds = productIds;
            SetUpdated();
        }

        public void Publish()
        {
            if(Status == PostStatus.Published)
                throw new DomainException("Bài viết đã được publish trước đó.");

            Status = PostStatus.Published;
            PublishedAt ??= DateTime.UtcNow;  // chỉ gán nếu chưa từng có giá trị
            SetUpdated();
        }

        public void Archive() 
        {
            if (Status == PostStatus.Draft)
                throw new DomainException("Không thể lưu trữ bài viết đang ở trạng thái Draft. Vui lòng xóa nếu không cần dùng nữa.");

            if (Status == PostStatus.Archived)
                throw new ConflictException("Bài viết đã được lưu trữ trước đó.");

            Status = PostStatus.Archived;
            SetUpdated();
        }

        public void IncreaseViewCount()
        {
            ViewCount++;
        }

    }
}
