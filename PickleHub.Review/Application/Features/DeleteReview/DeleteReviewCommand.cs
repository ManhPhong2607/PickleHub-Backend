using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Review.Application.Common.Interfaces;

namespace PickleHub.Review.Application.Features.DeleteReview;

public record DeleteReviewCommand(Guid ReviewId, Guid CurrentUserId, bool IsAdmin = false ) : IRequest;

public class DeleteReviewCommandHandler(IReviewDbContext db) : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand request, CancellationToken ct)
    {
        // 1. Tìm bài đánh giá theo ReviewId
        var review = await db.ProductReviews.FirstOrDefaultAsync(x => x.Id == request.ReviewId, ct);
        if (review is null)
        {
            throw new KeyNotFoundException($"Bài đánh giá có ID [{request.ReviewId}] không tồn tại.");
        }

        // 2. Phân quyền: Người xóa phải là chính chủ hoặc Admin
        bool isOwner = review.UserId == request.CurrentUserId;
        if (!isOwner && !request.IsAdmin)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xóa bài đánh giá này.");
        }

        // 3. Thực hiện Soft Delete (Xóa mềm để bảo toàn lịch sử Audit)
        review.IsDeleted = true;
        review.UpdatedAt = DateTime.UtcNow;

        // 4. Giảm mốc sao và tính toán lại Tổng quan Rating (ProductRating Summary)
        var ratingSummary = await db.ProductRatings.FirstOrDefaultAsync(r => r.ProductId == review.ProductId, ct);
        if (ratingSummary is not null && ratingSummary.TotalReviews > 0)
        {
            ratingSummary.TotalReviews -= 1;
            switch (review.Rating)
            {
                case 5: if (ratingSummary.FiveStar > 0) ratingSummary.FiveStar -= 1; break;
                case 4: if (ratingSummary.FourStar > 0) ratingSummary.FourStar -= 1; break;
                case 3: if (ratingSummary.ThreeStar > 0) ratingSummary.ThreeStar -= 1; break;
                case 2: if (ratingSummary.TwoStar > 0) ratingSummary.TwoStar -= 1; break;
                case 1: if (ratingSummary.OneStar > 0) ratingSummary.OneStar -= 1; break;
            }

            if (ratingSummary.TotalReviews == 0)
            {
                ratingSummary.AverageRating = 0.0;
            }
            else
            {
                var totalPoints = (ratingSummary.FiveStar * 5) + 
                                  (ratingSummary.FourStar * 4) + 
                                  (ratingSummary.ThreeStar * 3) + 
                                  (ratingSummary.TwoStar * 2) + 
                                  (ratingSummary.OneStar * 1);

                ratingSummary.AverageRating = Math.Round((double)totalPoints / ratingSummary.TotalReviews, 1);
            }

            ratingSummary.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
