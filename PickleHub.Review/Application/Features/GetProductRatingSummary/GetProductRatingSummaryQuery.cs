using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Review.Application.Common.Interfaces;
using PickleHub.Review.Application.DTOs;

namespace PickleHub.Review.Application.Features.GetProductRatingSummary;

public record GetProductRatingSummaryQuery(Guid ProductId) : IRequest<ProductRatingSummaryDto>;

public class GetProductRatingSummaryQueryHandler(IReviewDbContext db) : IRequestHandler<GetProductRatingSummaryQuery, ProductRatingSummaryDto>
{
    public async Task<ProductRatingSummaryDto> Handle(GetProductRatingSummaryQuery request, CancellationToken ct)
    {
        var summary = await db.ProductRatings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ProductId == request.ProductId, ct);

        if (summary is null)
        {
            return new ProductRatingSummaryDto(request.ProductId, 0.0, 0, 0, 0, 0, 0, 0);
        }

        return new ProductRatingSummaryDto(
            summary.ProductId,
            summary.AverageRating,
            summary.TotalReviews,
            summary.FiveStar,
            summary.FourStar,
            summary.ThreeStar,
            summary.TwoStar,
            summary.OneStar
        );
    }
}
