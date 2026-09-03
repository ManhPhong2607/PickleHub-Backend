using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Review.Application.Common.Interfaces;
using PickleHub.Review.Application.DTOs;

namespace PickleHub.Review.Application.Features.GetMyReviews;

public record GetMyReviewsQuery(Guid UserId) : IRequest<List<ReviewDto>>;

public class GetMyReviewsQueryHandler(IReviewDbContext db) : IRequestHandler<GetMyReviewsQuery, List<ReviewDto>>
{
    public async Task<List<ReviewDto>> Handle(GetMyReviewsQuery request, CancellationToken ct)
    {
        var reviews = await db.ProductReviews
            .Include(r => r.Images)
            .Where(r => r.UserId == request.UserId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return reviews.Select(r => new ReviewDto(
            r.Id,
            r.ProductId,
            r.ProductVariantId,
            r.UserId,
            null,
            r.OrderId,
            r.Rating,
            r.Comment,
            r.IsVerifiedPurchase,
            r.HelpfulCount,
            false,
            r.SellerReply,
            r.SellerRepliedAt,
            r.Images.Select(i => i.ImageUrl).ToList(),
            r.CreatedAt
        )).ToList();
    }
}
