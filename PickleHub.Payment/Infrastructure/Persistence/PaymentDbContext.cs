using Microsoft.EntityFrameworkCore;
using PickleHub.Payment.Application.Common.Interfaces;
using PickleHub.Payment.Domain.Entities;

namespace PickleHub.Payment.Infrastructure.Persistence;

public class PaymentDbContext : DbContext, IPaymentDbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payments> Payments => Set<Payments>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payments>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.OrderId);
            entity.HasIndex(p => p.OrderCode).IsUnique();
        });
    }
}
