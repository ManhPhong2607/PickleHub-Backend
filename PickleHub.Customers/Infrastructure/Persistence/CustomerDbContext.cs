using PickleHub.Common.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PickleHub.Customers.Domain.Entities;
using PickleHub.Customers.Domain.Repositories;
using System.Collections.Generic;
using System.Reflection.Emit;


namespace PickleHub.Customers.Infrastructure.Persistence
{
    public class CustomerDbContext : DbContext, IUnitOfWork
    {
        public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
        public DbSet<LoyaltyTier> LoyaltyTiers => Set<LoyaltyTier>();
        public DbSet<CustomerSpendLedger> CustomerSpendLedgers => Set<CustomerSpendLedger>();

        public void ClearTracking()
        {
            ChangeTracker.Clear();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(e =>
            {
                e.ToTable("customer");
                e.HasKey(c => c.Id);
                e.Property(c => c.UserId).IsRequired();
                e.HasIndex(c => c.UserId).IsUnique();
                e.Property(c => c.Email).IsRequired().HasMaxLength(255);
                e.HasIndex(c => c.Email).IsUnique();
                e.Property(c => c.FullName).IsRequired().HasMaxLength(200);
                e.Property(c => c.PhoneNumber).HasMaxLength(20);
                e.Property(c => c.IsBlocked).HasDefaultValue(false);
                e.Property(c => c.TotalSpent).HasColumnName("total_spent").HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(c => c.CreatedAt).HasColumnName("created_at");
                e.Property(c => c.UpdatedAt).HasColumnName("updated_at");

                e.Navigation(c => c.Addresses).HasField("_addresses");

                e.HasMany(c => c.Addresses)
                    .WithOne()
                    .HasForeignKey(a => a.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CustomerAddress>(e =>
            {
                e.ToTable("customer_address");
                e.HasKey(a => a.Id);
                e.Property(a => a.FullName).IsRequired().HasMaxLength(200);
                e.Property(a => a.PhoneNumber).IsRequired().HasMaxLength(20);
                e.Property(a => a.Province).IsRequired().HasMaxLength(100);
                e.Property(a => a.District).IsRequired().HasMaxLength(100);
                e.Property(a => a.Ward).IsRequired().HasMaxLength(100);
                e.Property(a => a.StreetAddress).IsRequired().HasMaxLength(300);
                e.Property(a => a.IsDefault).HasDefaultValue(false);
                e.Property(a => a.CreatedAt).HasColumnName("created_at");
                e.Property(a => a.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<LoyaltyTier>(e =>
            {
                e.ToTable("loyalty_tier");
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).IsRequired().HasMaxLength(100);
                e.Property(t => t.MinSpend).HasColumnName("min_spend").HasColumnType("decimal(18,2)");
                e.Property(t => t.DiscountPercent).HasColumnName("discount_percent").HasColumnType("decimal(5,2)");
                e.Property(t => t.SortOrder).HasColumnName("sort_order");
                e.Property(t => t.BenefitsJson).HasColumnName("benefits_json").HasColumnType("jsonb").HasDefaultValue("[]");
                e.Property(t => t.CreatedAt).HasColumnName("created_at");
                e.Property(t => t.UpdatedAt).HasColumnName("updated_at");

                var benefits = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    "Ưu đãi đặc quyền trong các dịp sinh nhật, ra mắt sản phẩm mới và những sự kiện khác.",
                    "Hỗ trợ chăm sóc tận tình trong suốt quá trình mua hàng."
                });
                var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                e.HasData(
                    new { Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"), Name = "Rookie", MinSpend = 3_000_000m, DiscountPercent = 5m, SortOrder = 1, BenefitsJson = benefits, CreatedAt = seedDate, UpdatedAt = (DateTime?)null },
                    new { Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"), Name = "Rally", MinSpend = 8_000_000m, DiscountPercent = 7m, SortOrder = 2, BenefitsJson = benefits, CreatedAt = seedDate, UpdatedAt = (DateTime?)null },
                    new { Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"), Name = "Ace", MinSpend = 20_000_000m, DiscountPercent = 10m, SortOrder = 3, BenefitsJson = benefits, CreatedAt = seedDate, UpdatedAt = (DateTime?)null },
                    new { Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"), Name = "Champion", MinSpend = 50_000_000m, DiscountPercent = 15m, SortOrder = 4, BenefitsJson = benefits, CreatedAt = seedDate, UpdatedAt = (DateTime?)null }
                );
            });

            modelBuilder.Entity<CustomerSpendLedger>(e =>
            {
                e.ToTable("customer_spend_ledger");
                e.HasKey(l => l.Id);
                e.Property(l => l.CustomerId).IsRequired();
                e.Property(l => l.OrderId).IsRequired();
                // 1 Order chỉ được ghi nhận đúng 1 lần - chặn cộng trùng khi event bị redeliver.
                e.HasIndex(l => l.OrderId).IsUnique();
                e.Property(l => l.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
                e.Property(l => l.CreatedAt).HasColumnName("created_at");
                e.Property(l => l.UpdatedAt).HasColumnName("updated_at");
            });
        }
    }
}
