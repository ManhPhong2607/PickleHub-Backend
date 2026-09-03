using PickleHub.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;

namespace PickleHub.AuditLog.Infrastructure.Persistence
{
    public class AuditLogDbContext : DbContext, IUnitOfWork
    {
        public AuditLogDbContext(DbContextOptions<AuditLogDbContext> options)
            : base(options) { }

        public DbSet<AuditLogs> AuditLogs => Set<AuditLogs>();

        public void ClearTracking()
        {
            ChangeTracker.Clear();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLogs>(e =>
            {
                e.ToTable("audit_log");
                e.HasKey(a => a.Id);
                e.Property(a => a.ActorId).HasColumnName("actor_id");
                e.Property(a => a.ActorRole).IsRequired().HasMaxLength(20)
                    .HasColumnName("actor_role");
                e.Property(a => a.ActorEmail).IsRequired().HasMaxLength(255)
                    .HasColumnName("actor_email");
                e.Property(a => a.Action).IsRequired().HasMaxLength(100);
                e.Property(a => a.EntityType).IsRequired().HasMaxLength(50)
                    .HasColumnName("entity_type");
                e.Property(a => a.EntityId).HasColumnName("entity_id");
                e.Property(a => a.Description).IsRequired();
                e.Property(a => a.Metadata).HasColumnType("jsonb");
                e.Property(a => a.OccurredAt).HasColumnName("occurred_at");
                e.Property(a => a.CreatedAt).HasColumnName("created_at");
                e.Property(a => a.UpdatedAt).HasColumnName("updated_at");

                // Index để query nhanh
                e.HasIndex(a => a.Action);
                e.HasIndex(a => a.EntityType);
                e.HasIndex(a => a.ActorId);
                e.HasIndex(a => a.OccurredAt);
            });
        }
    }
}
