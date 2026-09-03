namespace PickleHub.Review.Application.DTOs;

public record ReviewFilterDto(
    Guid ProductId,
    int? Rating = null,
    bool? HasImages = null,
    bool? VerifiedOnly = null,
    int PageNumber = 1,
    int PageSize = 10
);
