using PickleHub.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PickleHub.Catalog.Domain;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext, IUnitOfWork
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    //public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    //public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductViewDaily> ProductViewDailies => Set<ProductViewDaily>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();

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

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("category");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.Slug)
                .HasConversion(slugConverter)
                .HasMaxLength(300)
                .HasColumnName("slug");
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.AttributeSchemaJson)
               .HasColumnName("attribute_schema_json")
               .HasColumnType("jsonb")
               .HasDefaultValue("[]");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            e.HasMany(c => c.Children)
                .WithOne()
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Brand>(e =>
        {
            e.ToTable("brand");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).ValueGeneratedNever();
            e.Property(b => b.Name).IsRequired().HasMaxLength(200);
            e.Property(b => b.Slug)
                .HasConversion(slugConverter)
                .HasMaxLength(300)
                .HasColumnName("slug");
            e.HasIndex(b => b.Slug).IsUnique();
            e.Property(b => b.CreatedAt).HasColumnName("created_at");
            e.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("product");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Name).IsRequired().HasMaxLength(300);
            e.Property(p => p.Slug)
                .HasConversion(slugConverter)
                .HasMaxLength(300)
                .HasColumnName("slug");
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.BasePrice).HasColumnType("decimal(12,2)");
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.SpecsJson).HasColumnName("specs").HasColumnType("jsonb");
            e.Property(p => p.SoldCount).HasColumnName("sold_count").HasDefaultValue(0);
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");

            e.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Brand)
                .WithMany()
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Navigation(p => p.Images).HasField("_images");
            e.Navigation(p => p.Variants).HasField("_variants");
        });

        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.ToTable("product_variant");
            e.HasKey(v => v.Id);
            e.Property(v => v.Id).ValueGeneratedNever();
            e.Property(v => v.Sku).IsRequired().HasMaxLength(100);
            e.HasIndex(v => v.Sku).IsUnique();
            e.Property(v => v.AttributesJson).HasColumnName("attributes").HasColumnType("jsonb");
            e.Property(v => v.Price).HasColumnType("decimal(12,2)");
            e.Property(v => v.CreatedAt).HasColumnName("created_at");
            e.Property(v => v.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ProductImage>(e =>
        {
            e.ToTable("product_image");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).ValueGeneratedNever();
            e.Property(i => i.PublicId).IsRequired().HasColumnName("public_id").HasMaxLength(500);
            e.Property(i => i.Url).IsRequired();
            e.Property(i => i.IsSizeChart).HasColumnName("is_size_chart").HasDefaultValue(false);
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ProductViewDaily>(e =>
        {
            e.ToTable("product_view_daily");
            e.HasKey(v => v.Id);
            e.Property(v => v.Id).ValueGeneratedNever();
            e.Property(v => v.ViewDate).HasColumnName("view_date");
            e.Property(v => v.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
            e.Property(v => v.CreatedAt).HasColumnName("created_at");
            e.Property(v => v.UpdatedAt).HasColumnName("updated_at");
            // 1 sản phẩm chỉ có đúng 1 dòng cho mỗi ngày 
            e.HasIndex(v => new { v.ProductId, v.ViewDate }).IsUnique();
        });

        modelBuilder.Entity<Promotion>(e =>
        {
            e.ToTable("promotion");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Description).HasMaxLength(1000);
            e.Property(p => p.StartsAt).HasColumnName("starts_at");
            e.Property(p => p.EndsAt).HasColumnName("ends_at");
            e.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(p => p.Priority).HasColumnName("priority").HasDefaultValue(0);
            e.Ignore(p => p.IsCurrentlyRunning);
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            // Index cho đúng 3 điều kiện overlap check (IsActive, StartsAt, EndsAt) - đây là truy vấn quét nhiều nhất trong tính năng này (mỗi lần bulk-assign sản phẩm),
            // không có index này sẽ full-scan bảng promotion mỗi lần check overlap.
            e.HasIndex(p => new { p.IsActive, p.StartsAt, p.EndsAt });

            e.Navigation(p => p.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
            e.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.PromotionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromotionProduct>(e =>
        {
            e.ToTable("promotion_product");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).ValueGeneratedNever();
            e.Property(i => i.DiscountPercent).HasColumnName("discount_percent").HasColumnType("decimal(5,2)");
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");

            // 1 sản phẩm không được xuất hiện 2 lần trong CÙNG 1 Promotion.
            e.HasIndex(i => new { i.PromotionId, i.ProductId }).IsUnique();
            // Index riêng theo ProductId - phục vụ 2 truy vấn hay dùng nhất: (1) overlap check
            // theo danh sách ProductIds, (2) tính EffectivePrice hàng loạt lúc list sản phẩm.
            e.HasIndex(i => i.ProductId);
        });
    }
}
