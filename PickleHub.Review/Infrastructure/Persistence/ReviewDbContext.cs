using Microsoft.EntityFrameworkCore;
using PickleHub.Review.Application.Common.Interfaces;
using PickleHub.Review.Domain.Entities;

namespace PickleHub.Review.Infrastructure.Persistence;

public class ReviewDbContext : DbContext, IReviewDbContext
{
    public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options)
    {
    }

    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ProductRating> ProductRatings => Set<ProductRating>();
    public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();
    public DbSet<ReviewLike> ReviewLikes => Set<ReviewLike>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Comment)
                .HasMaxLength(2000);

            entity.Property(e => e.SellerReply)
                .HasMaxLength(2000);

            entity.Property(e => e.HideReason)
                .HasMaxLength(500);

            // Index tối ưu tốc độ đọc danh sách đánh giá theo sản phẩm
            entity.HasIndex(e => new { e.ProductId, e.IsDeleted, e.IsHidden, e.CreatedAt })
                .HasDatabaseName("idx_product_reviews_product_created");

            // Partial Unique Index ở Postgres level: 
            // Đảm bảo 1 User + 1 Order + 1 Product chỉ được tạo 1 review (Bỏ qua các bài review đã Soft Delete)
            entity.HasIndex(e => new { e.UserId, e.OrderId, e.ProductId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("idx_reviews_user_order_product_active_unique");

            // Soft Delete & Moderation Global Query Filter (Chỉ hiển thị bài chưa xóa và chưa bị ẩn)
            entity.HasQueryFilter(e => !e.IsDeleted && !e.IsHidden);

            // Cascade Delete quan hệ Hình ảnh & Like khi xoá Đánh giá
            entity.HasMany(e => e.Images)
                .WithOne(i => i.Review)
                .HasForeignKey(i => i.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Likes)
                .WithOne(l => l.Review)
                .HasForeignKey(l => l.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<ProductRating>(entity =>
        {
            entity.HasKey(e => e.ProductId);

            entity.Property(e => e.AverageRating)
                .HasDefaultValue(0.0);

            entity.Property(e => e.TotalReviews)
                .HasDefaultValue(0);
        });
        
        modelBuilder.Entity<ReviewImage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(e => e.ReviewId)
                .HasDatabaseName("idx_review_images_review_id");
        });
        
        modelBuilder.Entity<ReviewLike>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique Index: Đảm bảo 1 User chỉ được thả Like 1 lần duy nhất cho 1 Đánh giá
            entity.HasIndex(e => new { e.ReviewId, e.UserId })
                .IsUnique()
                .HasDatabaseName("idx_review_likes_review_user_unique");
        });
    }
}
