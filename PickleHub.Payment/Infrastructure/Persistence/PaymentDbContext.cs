using Microsoft.EntityFrameworkCore;
using PickleHub.Payment.Application.Common.Interfaces;
using PickleHub.Payment.Domain.Entities;

namespace PickleHub.Payment.Infrastructure.Persistence;

public class PaymentDbContext : DbContext, IPaymentDbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payments> Payments => Set<Payments>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payments>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.OrderId);
            entity.HasIndex(p => p.OrderCode).IsUnique();
        });

        modelBuilder.Entity<RefundRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.OrderId);
            entity.HasIndex(r => r.Status);
            entity.HasOne(r => r.Payment)
                  .WithMany()
                  .HasForeignKey(r => r.PaymentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
