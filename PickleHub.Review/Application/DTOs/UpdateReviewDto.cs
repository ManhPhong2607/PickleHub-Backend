namespace PickleHub.Review.Application.DTOs;

// DTO hứng dữ liệu khi Client gửi yêu cầu cập nhật bài đánh giá
public record UpdateReviewDto(
    int Rating,
    string? Comment,
    List<string>? ImageUrls
);
