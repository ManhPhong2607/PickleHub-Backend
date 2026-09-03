namespace PickleHub.Review.Domain.Entities;

public class ReviewImage
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProductReview? Review { get; set; }
}
