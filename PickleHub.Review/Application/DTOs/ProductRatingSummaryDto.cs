namespace PickleHub.Review.Application.DTOs;

public record ProductRatingSummaryDto(
    Guid ProductId,
    double AverageRating,
    int TotalReviews,
    int FiveStarCount,
    int FourStarCount,
    int ThreeStarCount,
    int TwoStarCount,
    int OneStarCount
);
