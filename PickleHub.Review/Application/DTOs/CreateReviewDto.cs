namespace PickleHub.Review.Application.DTOs;

public record CreateReviewDto(
    Guid ProductId,
    Guid OrderId,
    Guid? ProductVariantId,
    int Rating,
    string? Comment,
    List<string>? ImageUrls
);
