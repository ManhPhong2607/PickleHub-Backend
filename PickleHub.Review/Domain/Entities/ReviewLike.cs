namespace PickleHub.Review.Domain.Entities;

public class ReviewLike
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ReviewId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProductReview? Review { get; set; }
}
