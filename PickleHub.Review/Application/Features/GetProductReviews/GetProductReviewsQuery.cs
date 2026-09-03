using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Common.DTOs;
using PickleHub.Review.Application.Common.Interfaces;
using PickleHub.Review.Application.DTOs;

namespace PickleHub.Review.Application.Features.GetProductReviews;

public record GetProductReviewsQuery(
    Guid ProductId,
    int? Rating = null,
    bool? HasImages = null,
    bool? VerifiedOnly = null,
    Guid? CurrentUserId = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<PagedResult<ReviewDto>>;

public class GetProductReviewsQueryHandler(IReviewDbContext db) : IRequestHandler<GetProductReviewsQuery, PagedResult<ReviewDto>>
{
    public async Task<PagedResult<ReviewDto>> Handle(GetProductReviewsQuery request, CancellationToken ct)
    {
        // Sử dụng AsNoTracking() tối ưu hiệu năng đọc DB
        var query = db.ProductReviews
            .AsNoTracking()
            .Include(r => r.Images)
            .Include(r => r.Likes)
            .Where(r => r.ProductId == request.ProductId);

        if (request.Rating.HasValue && request.Rating.Value >= 1 && request.Rating.Value <= 5)
        {
            query = query.Where(r => r.Rating == request.Rating.Value);
        }

        if (request.HasImages == true)
        {
            query = query.Where(r => r.Images.Count > 0);
        }

        if (request.VerifiedOnly == true)
        {
            query = query.Where(r => r.IsVerifiedPurchase);
        }

        var totalItems = await query.CountAsync(ct);

        // Clamp kích thước trang (PageSize tối đa 50 bài/trang)
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = Math.Min(Math.Max(request.PageSize, 1), 50);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var reviewDtos = reviews.Select(r => new ReviewDto(
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
            request.CurrentUserId.HasValue && r.Likes.Any(l => l.UserId == request.CurrentUserId.Value),
            r.SellerReply,
            r.SellerRepliedAt,
            r.Images.Select(i => i.ImageUrl).ToList(),
            r.CreatedAt
        )).ToList();

        return new PagedResult<ReviewDto>
        {
            Items = reviewDtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
}
