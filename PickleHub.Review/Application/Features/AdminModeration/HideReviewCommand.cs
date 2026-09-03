using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Review.Application.Common.Interfaces;

namespace PickleHub.Review.Application.Features.AdminModeration;

// Command cho Admin Ẩn / Bỏ ẩn bài đánh giá vi phạm quy định
public record HideReviewCommand(
    Guid ReviewId,
    bool IsHidden,
    string? Reason = null
) : IRequest;

public class HideReviewCommandHandler(IReviewDbContext db) : IRequestHandler<HideReviewCommand>
{
    public async Task Handle(HideReviewCommand request, CancellationToken ct)
    {
        using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // Sử dụng IgnoreQueryFilters để Admin tìm được cả các bài review đang bị ẩn
            var review = await db.ProductReviews
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == request.ReviewId && !r.IsDeleted, ct);

            if (review is null)
            {
                throw new KeyNotFoundException($"Bài đánh giá có ID [{request.ReviewId}] không tồn tại.");
            }

            // Nếu trạng thái IsHidden không thay đổi thì giữ nguyên
            if (review.IsHidden == request.IsHidden)
            {
                return;
            }

            review.IsHidden = request.IsHidden;
            review.HideReason = request.IsHidden ? request.Reason?.Trim() : null;
            review.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            // Cập nhật lại ProductRating summary cho sản phẩm (Loại trừ bài review bị Ẩn)
            var activeReviews = await db.ProductReviews
                .Where(r => r.ProductId == review.ProductId && !r.IsDeleted && !r.IsHidden)
                .ToListAsync(ct);

            var ratingSummary = await db.ProductRatings
                .FirstOrDefaultAsync(r => r.ProductId == review.ProductId, ct);

            if (ratingSummary is not null)
            {
                ratingSummary.TotalReviews = activeReviews.Count;
                ratingSummary.FiveStar = activeReviews.Count(r => r.Rating == 5);
                ratingSummary.FourStar = activeReviews.Count(r => r.Rating == 4);
                ratingSummary.ThreeStar = activeReviews.Count(r => r.Rating == 3);
                ratingSummary.TwoStar = activeReviews.Count(r => r.Rating == 2);
                ratingSummary.OneStar = activeReviews.Count(r => r.Rating == 1);

                if (ratingSummary.TotalReviews > 0)
                {
                    var totalPoints = (ratingSummary.FiveStar * 5) +
                                      (ratingSummary.FourStar * 4) +
                                      (ratingSummary.ThreeStar * 3) +
                                      (ratingSummary.TwoStar * 2) +
                                      (ratingSummary.OneStar * 1);

                    ratingSummary.AverageRating = Math.Round((double)totalPoints / ratingSummary.TotalReviews, 1);
                }
                else
                {
                    ratingSummary.AverageRating = 0.0;
                }

                ratingSummary.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
