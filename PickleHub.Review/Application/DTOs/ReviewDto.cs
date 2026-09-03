namespace PickleHub.Review.Application.DTOs;

public record ReviewDto(
    Guid Id,
    Guid ProductId,
    Guid? ProductVariantId,
    Guid UserId,
    string? UserName,
    Guid? OrderId,
    int Rating,
    string? Comment,
    bool IsVerifiedPurchase,
    int HelpfulCount,
    bool IsLikedByCurrentUser,
    string? SellerReply,
    DateTime? SellerRepliedAt,
    List<string> ImageUrls,
    DateTime CreatedAt
);
