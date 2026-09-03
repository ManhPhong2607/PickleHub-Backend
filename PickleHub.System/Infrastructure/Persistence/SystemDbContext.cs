using PickleHub.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using PickleHub.System.Domain.Entities;
using PickleHub.System.Domain.Repositories;

namespace PickleHub.System.Infrastructure.Persistence
{
    public class SystemDbContext : DbContext, IUnitOfWork
    {
        public SystemDbContext(DbContextOptions<SystemDbContext> options) : base(options) { }

        public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
        public DbSet<SiteAnnouncement> SiteAnnouncements => Set<SiteAnnouncement>();
        public void ClearTracking()
        {
            ChangeTracker.Clear();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemConfig>(e =>
            {
                e.ToTable("system_config");
                e.HasKey(c => c.Id);
                e.Property(c => c.Key).IsRequired().HasMaxLength(100);
                e.HasIndex(c => c.Key).IsUnique();
                e.Property(c => c.Value).IsRequired().HasMaxLength(500);
                e.Property(c => c.Description).HasMaxLength(500);
                e.Property(c => c.CreatedAt).HasColumnName("created_at");
                e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<SiteAnnouncement>(e =>
            {
                e.ToTable("site_announcement");
                e.HasKey(a => a.Id);
                e.Property(a => a.Title).IsRequired().HasMaxLength(200);
                e.Property(a => a.Content).IsRequired().HasMaxLength(1000);
                e.Property(a => a.IsActive).HasDefaultValue(true);
                e.Property(a => a.StartsAt).HasColumnName("starts_at");
                e.Property(a => a.EndsAt).HasColumnName("ends_at");
                e.Property(a => a.ImageUrl).HasColumnName("image_url").HasMaxLength(1000);
                e.Property(a => a.ImagePublicId).HasColumnName("image_public_id").HasMaxLength(500);
                e.Property(a => a.CtaLink).HasColumnName("cta_link").HasMaxLength(1000);
                e.Property(a => a.CreatedAt).HasColumnName("created_at");
                e.Property(a => a.UpdatedAt).HasColumnName("updated_at");

                // IsVisible là computed property, không map vào DB
                e.Ignore(a => a.IsVisible);
            });
        }
    }
}
