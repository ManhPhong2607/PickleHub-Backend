using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PickleHub.Review.Domain.Entities;

namespace PickleHub.Review.Application.Common.Interfaces;

public interface IReviewDbContext
{
    DbSet<ProductReview> ProductReviews { get; }
    DbSet<ProductRating> ProductRatings { get; }
    DbSet<ReviewImage> ReviewImages { get; }
    DbSet<ReviewLike> ReviewLikes { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
