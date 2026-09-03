using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Review.Application.Common.Interfaces;
using PickleHub.Review.Application.DTOs;

namespace PickleHub.Review.Application.Features.AdminReply;

public record AdminReplyReviewCommand(Guid ReviewId, string ReplyContent) : IRequest<ReviewDto>;
    
public class AdminReplyReviewCommandHandler(IReviewDbContext db) : IRequestHandler<AdminReplyReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(AdminReplyReviewCommand request, CancellationToken ct)
    {
        // 1. Tìm bài đánh giá theo ReviewId (Sử dụng IgnoreQueryFilters để Admin phản hồi được bài bị ẩn)
        var review = await db.ProductReviews
            .IgnoreQueryFilters()
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId && !r.IsDeleted, ct);
        
        if (review is null)
        {
            throw new KeyNotFoundException($"Không tìm thấy bài đánh giá có ID [{request.ReviewId}].");
        }
        
        // 2. Cập nhật phản hồi của Admin
        review.SellerReply = request.ReplyContent.Trim();
        review.SellerRepliedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;
        
        // 3. Lưu vào Database
        await db.SaveChangesAsync(ct);
        
        // 4. Trả về ReviewDto đã được cập nhật
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
