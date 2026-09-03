using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Review.Application.Common.Interfaces;
using PickleHub.Review.Domain.Entities;

namespace PickleHub.Review.Application.Features.ToggleLikeReview;

public record ToggleLikeReviewCommand(Guid ReviewId, Guid UserId) : IRequest<bool>;

public class ToggleLikeReviewCommandHandler(IReviewDbContext db) : IRequestHandler<ToggleLikeReviewCommand, bool>
{
    public async Task<bool> Handle(ToggleLikeReviewCommand request, CancellationToken ct)
    {
        using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var review = await db.ProductReviews.FirstOrDefaultAsync(r => r.Id == request.ReviewId, ct);
            if (review is null)
            {
                throw new KeyNotFoundException($"Bài đánh giá có ID [{request.ReviewId}] không tồn tại.");
            }

            var existingLike = await db.ReviewLikes.FirstOrDefaultAsync(l => l.ReviewId == request.ReviewId && l.UserId == request.UserId, ct);
            bool isLiked;

            if (existingLike is not null)
            {
                db.ReviewLikes.Remove(existingLike);
                isLiked = false;
            }
            else
            {
                db.ReviewLikes.Add(new ReviewLike
                {
                    Id = Guid.NewGuid(),
                    ReviewId = request.ReviewId,
                    UserId = request.UserId,
                    CreatedAt = DateTime.UtcNow
                });
                isLiked = true;
            }

            await db.SaveChangesAsync(ct);

            // Đồng bộ trực tiếp HelpfulCount từ số đếm thực tế trong DB (Chống lệch dữ liệu khi có nhiều request đồng thời)
            review.HelpfulCount = await db.ReviewLikes.CountAsync(l => l.ReviewId == request.ReviewId, ct);
            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            return isLiked;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
