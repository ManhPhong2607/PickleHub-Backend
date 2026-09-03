using PickleHub.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Repositories;
using Npgsql;
using PickleHub.Common.Exceptions;

namespace PickleHub.Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext, IUnitOfWork
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<StockTransaction>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await base.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "Dữ liệu tồn kho đã bị thay đổi bởi một thao tác khác trong lúc xử lý.", ex);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateOperationException(
                "Thao tác này đã được thực hiện trước đó.", ex);
        }
    }
    public void ClearTracking()
    {
        ChangeTracker.Clear();
    }
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pgEx
            && pgEx.SqlState == PostgresErrorCodes.UniqueViolation;
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.ToTable("inventory_item");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.ProductVariantId).IsRequired()
                .HasColumnName("product_variant_id");
            e.HasIndex(i => i.ProductVariantId).IsUnique();
            e.Property(i => i.ProductId).IsRequired()
                .HasColumnName("product_id");
            e.Property(i => i.SkuSnapshot).IsRequired().HasMaxLength(100)
                .HasColumnName("sku_snapshot");
            e.Property(i => i.Quantity).HasColumnName("quantity").HasDefaultValue(0);
            e.Property(i => i.ReservedQuantity).HasDefaultValue(0)
                .HasColumnName("reserved_quantity");
            e.Property(i => i.LowStockThreshold)
                .HasDefaultValue(5)
                .HasColumnName("low_stock_threshold");
            e.Property(i => i.CreatedAt).HasColumnName("created_at");
            e.Property(i => i.UpdatedAt).HasColumnName("updated_at");
            e.Property(i => i.Version)
                .HasColumnName("version")
                .HasDefaultValue(0u)
                .IsConcurrencyToken();

            // Computed properties — không map vào DB
            e.Ignore(i => i.IsLowStock);
            e.Ignore(i => i.IsOutOfStock);
            e.Ignore(i => i.AvailableQuantity);

            e.Navigation(i => i.Transactions).HasField("_transactions");

            e.HasMany(i => i.Transactions)
                .WithOne()
                .HasForeignKey(t => t.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockTransaction>(e =>
        {
            e.ToTable("stock_transaction");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.InventoryItemId)
                .HasColumnName("inventory_item_id");
            e.Property(t => t.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .HasMaxLength(20);
            e.Property(t => t.Quantity).HasColumnName("quantity").IsRequired();
            e.Property(t => t.ReferenceId).HasColumnName("reference_id");
            e.Property(t => t.Note).HasColumnName("note").HasMaxLength(500);
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(t => new { t.InventoryItemId, t.Type, t.ReferenceId })
                .IsUnique()
                .HasFilter("\"reference_id\" IS NOT NULL")
                .HasDatabaseName("ix_stock_transaction_idempotency");
        });
    }
}
