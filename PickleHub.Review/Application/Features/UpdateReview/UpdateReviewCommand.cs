using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Review.Application.Common.Interfaces;
using PickleHub.Review.Application.DTOs;
using PickleHub.Review.Domain.Entities;

namespace PickleHub.Review.Application.Features.UpdateReview;

// Command yêu cầu chỉnh sửa bài đánh giá đã gửi (Chỉ chính chủ người tạo UserId mới được sửa)
public record UpdateReviewCommand(
    Guid ReviewId,
    Guid UserId,
    int Rating,
    string? Comment,
    List<string>? ImageUrls
) : IRequest<ReviewDto>;

public class UpdateReviewCommandHandler(IReviewDbContext db) : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(UpdateReviewCommand request, CancellationToken ct)
    {
        // 1. Tìm bài đánh giá trong Database
        var review = await db.ProductReviews
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, ct);

        if (review is null)
        {
            throw new KeyNotFoundException($"Bài đánh giá có ID [{request.ReviewId}] không tồn tại.");
        }

        // 2. Kiểm tra chính chủ (User chỉ được sửa bài đánh giá của chính mình)
        if (review.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bài đánh giá này.");
        }
        //giới hạn thời gian sửa bài <7 ngày
        if (review.CreatedAt.AddDays(7) < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Bài đánh giá đã quá thời hạn 7 ngày, không thể chỉnh sửa.");
        }

        // 3. Nếu số sao thay đổi -> Cập nhật lại thống kê điểm số trung bình (ProductRating Summary)
        if (review.Rating != request.Rating)
        {
            var ratingSummary = await db.ProductRatings.FirstOrDefaultAsync(r => r.ProductId == review.ProductId, ct);
            if (ratingSummary is not null && ratingSummary.TotalReviews > 0)
            {
                // Giảm đếm số sao mốc cũ
                switch (review.Rating)
                {
                    case 5: if (ratingSummary.FiveStar > 0) ratingSummary.FiveStar -= 1; break;
                    case 4: if (ratingSummary.FourStar > 0) ratingSummary.FourStar -= 1; break;
                    case 3: if (ratingSummary.ThreeStar > 0) ratingSummary.ThreeStar -= 1; break;
                    case 2: if (ratingSummary.TwoStar > 0) ratingSummary.TwoStar -= 1; break;
                    case 1: if (ratingSummary.OneStar > 0) ratingSummary.OneStar -= 1; break;
                }

                // Tăng đếm số sao mốc mới
                switch (request.Rating)
                {
                    case 5: ratingSummary.FiveStar += 1; break;
                    case 4: ratingSummary.FourStar += 1; break;
                    case 3: ratingSummary.ThreeStar += 1; break;
                    case 2: ratingSummary.TwoStar += 1; break;
                    case 1: ratingSummary.OneStar += 1; break;
                }

                // Tính lại điểm số trung bình chuẩn
                var totalPoints = (ratingSummary.FiveStar * 5) +
                                  (ratingSummary.FourStar * 4) +
                                  (ratingSummary.ThreeStar * 3) +
                                  (ratingSummary.TwoStar * 2) +
                                  (ratingSummary.OneStar * 1);

                ratingSummary.AverageRating = Math.Round((double)totalPoints / ratingSummary.TotalReviews, 1);
                ratingSummary.UpdatedAt = DateTime.UtcNow;
            }
        }

        // 4. Cập nhật các thông tin bài đánh giá
        review.Rating = request.Rating;
        review.Comment = request.Comment?.Trim();
        review.UpdatedAt = DateTime.UtcNow;

        // 5. Cập nhật lại danh sách hình ảnh đính kèm (Xóa danh sách ảnh cũ & thay bằng danh sách ảnh mới)
        db.ReviewImages.RemoveRange(review.Images);
        review.Images.Clear();

        if (request.ImageUrls is not null && request.ImageUrls.Count > 0)
        {
            foreach (var url in request.ImageUrls)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    review.Images.Add(new ReviewImage
                    {
                        Id = Guid.NewGuid(),
                        ReviewId = review.Id,
                        ImageUrl = url.Trim(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // 6. Lưu toàn bộ thay đổi vào Database
        await db.SaveChangesAsync(ct);

        // 7. Trả về DTO cập nhật mới nhất cho Client
        return new ReviewDto(
            review.Id,
            review.ProductId,
            review.ProductVariantId,
            review.UserId,
            null,
            review.OrderId,
            review.Rating,
            review.Comment,
            review.IsVerifiedPurchase,
            review.HelpfulCount,
            false,
            review.SellerReply,
            review.SellerRepliedAt,
            review.Images.Select(i => i.ImageUrl).ToList(),
            review.CreatedAt
        );
    }
}
