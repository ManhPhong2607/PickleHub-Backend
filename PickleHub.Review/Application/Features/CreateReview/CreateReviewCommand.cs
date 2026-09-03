using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Review.Application.Common.Interfaces;
using PickleHub.Review.Application.DTOs;
using PickleHub.Review.Domain.Entities;
using PickleHub.Review.Domain.Interfaces;

namespace PickleHub.Review.Application.Features.CreateReview;

// Command tạo bài đánh giá
public record CreateReviewCommand(
    Guid UserId,
    Guid ProductId,
    Guid OrderId,
    Guid? ProductVariantId,
    int Rating,
    string? Comment,
    List<string>? ImageUrls
) : IRequest<ReviewDto>;

public class CreateReviewCommandHandler(IReviewDbContext db, IOrderClient orderClient) : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        var validImageUrls = request.ImageUrls?
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .ToList() ?? new List<string>();

        // 1. Kiểm tra Idempotency chuẩn (1 User + 1 Order + 1 Product chỉ được 1 bài Review)
        bool isAlreadyReviewed = await db.ProductReviews.AnyAsync(r => 
            r.UserId == request.UserId && 
            r.OrderId == request.OrderId && 
            r.ProductId == request.ProductId, ct);

        if (isAlreadyReviewed)
        {
            throw new InvalidOperationException("Bạn đã gửi đánh giá cho sản phẩm trong đơn hàng này trước đó.");
        }

        // 2. Verify Đơn hàng thực tế từ CartOrder Service (Rule FR-26)
        bool isVerifiedPurchase = await orderClient.VerifyOrderCompletedAsync(
            request.UserId, request.OrderId, request.ProductId, ct);

        if (!isVerifiedPurchase)
        {
            throw new InvalidOperationException("Đơn hàng chưa hoàn tất hoặc sản phẩm không thuộc đơn hàng chỉ định.");
        }

        // 3. Khởi tạo Explicit DB Transaction
        using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var reviewId = Guid.NewGuid();

            var review = new ProductReview
            {
                Id = reviewId,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                UserId = request.UserId,
                OrderId = request.OrderId,
                Rating = request.Rating,
                Comment = request.Comment?.Trim(),
                IsVerifiedPurchase = true,
                HelpfulCount = 0,
                IsDeleted = false,
                IsHidden = false,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var url in validImageUrls)
            {
                review.Images.Add(new ReviewImage
                {
                    Id = Guid.NewGuid(),
                    ReviewId = reviewId,
                    ImageUrl = url,
                    CreatedAt = DateTime.UtcNow
                });
            }

            db.ProductReviews.Add(review);

            // 4. Cập nhật Tổng quan Rating (ProductRating Summary)
            var ratingSummary = await db.ProductRatings.FirstOrDefaultAsync(r => r.ProductId == request.ProductId, ct);
            if (ratingSummary is null)
            {
                ratingSummary = new ProductRating
                {
                    ProductId = request.ProductId,
                    TotalReviews = 1,
                    FiveStar = request.Rating == 5 ? 1 : 0,
                    FourStar = request.Rating == 4 ? 1 : 0,
                    ThreeStar = request.Rating == 3 ? 1 : 0,
                    TwoStar = request.Rating == 2 ? 1 : 0,
                    OneStar = request.Rating == 1 ? 1 : 0,
                    AverageRating = request.Rating,
                    UpdatedAt = DateTime.UtcNow
                };
                db.ProductRatings.Add(ratingSummary);
            }
            else
            {
                ratingSummary.TotalReviews += 1;
                switch (request.Rating)
                {
                    case 5: ratingSummary.FiveStar += 1; break;
                    case 4: ratingSummary.FourStar += 1; break;
                    case 3: ratingSummary.ThreeStar += 1; break;
                    case 2: ratingSummary.TwoStar += 1; break;
                    case 1: ratingSummary.OneStar += 1; break;
                }

                var totalPoints = (ratingSummary.FiveStar * 5) + 
                                  (ratingSummary.FourStar * 4) + 
                                  (ratingSummary.ThreeStar * 3) + 
                                  (ratingSummary.TwoStar * 2) + 
                                  (ratingSummary.OneStar * 1);

                ratingSummary.AverageRating = Math.Round((double)totalPoints / ratingSummary.TotalReviews, 1);
                ratingSummary.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

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
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("idx_reviews_user_order_product_active_unique") == true)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Bạn đã gửi đánh giá cho sản phẩm trong đơn hàng này trước đó (Race Condition Detected).", ex);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
