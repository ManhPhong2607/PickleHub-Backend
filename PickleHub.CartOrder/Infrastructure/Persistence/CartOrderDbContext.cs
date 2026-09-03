using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Domain.Entities;

namespace PickleHub.CartOrder.Infrastructure.Persistence;

public class CartOrderDbContext : DbContext, ICartOrderDbContext
{
    public CartOrderDbContext(DbContextOptions<CartOrderDbContext> options) : base(options) { }

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public void ClearTracking()
    {
        ChangeTracker.Clear();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.UserId);
            entity.HasIndex(c => c.SessionId);
            entity.HasMany(c => c.Items)
                  .WithOne(i => i.Cart)
                  .HasForeignKey(i => i.CartId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => new { i.CartId, i.ProductVariantId }).IsUnique(); 
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.CustomerId);
            entity.HasIndex(o => o.Status);
            entity.HasMany(o => o.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
        });
    }
}
