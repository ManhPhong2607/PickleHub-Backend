using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Common.DTOs;
using PickleHub.Review.Application.Common.Interfaces;
using PickleHub.Review.Application.DTOs;

namespace PickleHub.Review.Application.Features.GetAdminReviews;

public record GetAdminReviewsQuery(
    string? Keyword = null,
    Guid? ProductId = null,
    int? Rating = null,
    bool? IsHidden = null,
    bool? HasReply = null,
    bool? HasImages = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SortBy = "newest",
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<AdminReviewItemDto>>;

public class GetAdminReviewsQueryHandler(IReviewDbContext db) : IRequestHandler<GetAdminReviewsQuery, PagedResult<AdminReviewItemDto>>
{
    public async Task<PagedResult<AdminReviewItemDto>> Handle(GetAdminReviewsQuery request, CancellationToken ct)
    {
        // Admin xem toàn bộ bài đánh giá kể cả bị ẩn (IgnoreQueryFilters)
        var query = db.ProductReviews
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(r => r.Images)
            .Where(r => !r.IsDeleted);

        // 1. Lọc theo từ khóa (Keyword)
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLower();
            query = query.Where(r => 
                (r.Comment != null && r.Comment.ToLower().Contains(kw)) ||
                (r.SellerReply != null && r.SellerReply.ToLower().Contains(kw)) ||
                (r.HideReason != null && r.HideReason.ToLower().Contains(kw)));
        }

        // 2. Lọc theo sản phẩm (ProductId)
        if (request.ProductId.HasValue && request.ProductId.Value != Guid.Empty)
        {
            query = query.Where(r => r.ProductId == request.ProductId.Value);
        }

        // 3. Lọc theo số sao đánh giá (Rating)
        if (request.Rating.HasValue && request.Rating.Value >= 1 && request.Rating.Value <= 5)
        {
            query = query.Where(r => r.Rating == request.Rating.Value);
        }

        // 4. Lọc theo trạng thái ẩn/hiện (IsHidden)
        if (request.IsHidden.HasValue)
        {
            query = query.Where(r => r.IsHidden == request.IsHidden.Value);
        }

        // 5. Lọc theo trạng thái phản hồi của Shop (HasReply)
        if (request.HasReply.HasValue)
        {
            if (request.HasReply.Value)
            {
                query = query.Where(r => !string.IsNullOrEmpty(r.SellerReply));
            }
            else
            {
                query = query.Where(r => string.IsNullOrEmpty(r.SellerReply));
            }
        }

        // 6. Lọc theo bài đánh giá có đính kèm hình ảnh (HasImages)
        if (request.HasImages.HasValue)
        {
            if (request.HasImages.Value)
            {
                query = query.Where(r => r.Images.Count > 0);
            }
            else
            {
                query = query.Where(r => r.Images.Count == 0);
            }
        }

        // 7. Lọc theo khoảng ngày (FromDate & ToDate)
        if (request.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(request.FromDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(r => r.CreatedAt >= fromUtc);
        }

        if (request.ToDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(request.ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(r => r.CreatedAt <= toUtc);
        }

        // 8. Đếm tổng số bản ghi
        var totalItems = await query.CountAsync(ct);

        // 9. Sắp xếp (Sorting)
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(r => r.CreatedAt),
            "rating_asc" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "rating_desc" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "helpful" => query.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        // 10. Phân trang
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = Math.Min(Math.Max(request.PageSize, 1), 100);

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = reviews.Select(r => new AdminReviewItemDto
        {
            Id = r.Id,
            ProductId = r.ProductId,
            ProductVariantId = r.ProductVariantId,
            UserId = r.UserId,
            OrderId = r.OrderId,
            Rating = r.Rating,
            Comment = r.Comment,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            HelpfulCount = r.HelpfulCount,
            IsHidden = r.IsHidden,
            HideReason = r.HideReason,
            SellerReply = r.SellerReply,
            SellerRepliedAt = r.SellerRepliedAt,
            ImageUrls = r.Images.Select(i => i.ImageUrl).ToList(),
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        return new PagedResult<AdminReviewItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
}
