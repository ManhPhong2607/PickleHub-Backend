namespace PickleHub.Review.Domain.Entities;

public class ProductReview
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public bool IsVerifiedPurchase { get; set; }

    public int HelpfulCount { get; set; }
    public bool IsHidden { get; set; }
    public string? HideReason { get; set; }
    public string? SellerReply { get; set; }
    public DateTime? SellerRepliedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
    public ICollection<ReviewLike> Likes { get; set; } = new List<ReviewLike>();
}
