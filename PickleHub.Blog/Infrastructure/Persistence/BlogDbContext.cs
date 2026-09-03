using PickleHub.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PickleHub.Common.ValueObjects;
using PickleHub.Blog.Domain.Entities;

namespace PickleHub.Blog.Infrastructure.Persistence
{
    public class BlogDbContext : DbContext, IUnitOfWork
    {
        public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options) { }

        public DbSet<ContentCategory> Categories => Set<ContentCategory>();
        public DbSet<Post> Posts => Set<Post>();
        public void ClearTracking()
        {
            ChangeTracker.Clear();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var slugConverter = new ValueConverter<Slug, string>(
                slug => slug.Value,
                value => Slug.FromPersistedValue(value)
            );

            modelBuilder.Entity<ContentCategory>(e =>
            {
                e.ToTable("content_category");
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedNever();
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);
                e.Property(c => c.Slug)
                    .HasConversion(slugConverter)
                    .HasMaxLength(120)
                    .HasColumnName("slug");
                e.HasIndex(c => c.Slug).IsUnique();
                e.Property(c => c.Description).HasMaxLength(500);
                e.Property(c => c.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
                e.Property(c => c.CreatedAt).HasColumnName("created_at");
                e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<Post>(e =>
            {
                e.ToTable("post");
                e.HasKey(p => p.Id);
                e.Property(p => p.Id).ValueGeneratedNever();
                e.Property(p => p.Title).IsRequired().HasMaxLength(200);
                e.Property(p => p.Slug)
                    .HasConversion(slugConverter)
                    .HasMaxLength(220)
                    .HasColumnName("slug");
                e.HasIndex(p => p.Slug).IsUnique();
                e.Property(p => p.Summary).HasMaxLength(500);
                e.Property(p => p.Content).IsRequired();

                e.Property(p => p.CoverImageUrl).HasColumnName("cover_image_url").HasMaxLength(500);
                e.Property(p => p.CoverImagePublicId).HasColumnName("cover_image_public_id").HasMaxLength(200);

                e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
                e.Property(p => p.PublishedAt).HasColumnName("published_at");

                e.Property(p => p.AuthorId).HasColumnName("author_id");
                e.Property(p => p.ViewCount).HasColumnName("view_count").HasDefaultValue(0);

                e.Property(p => p.SeoTitle).HasColumnName("seo_title").HasMaxLength(70);
                e.Property(p => p.SeoDescription).HasColumnName("seo_description").HasMaxLength(160);

                e.Property(p => p.RelatedProductIds)
                    .HasColumnName("related_product_ids")
                    .HasColumnType("uuid[]");

                e.Property(p => p.CreatedAt).HasColumnName("created_at");
                e.Property(p => p.UpdatedAt).HasColumnName("updated_at");

                e.HasOne(p => p.Category)
                    .WithMany()
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(p => new { p.Status, p.PublishedAt });
            });
        }
    }
}
