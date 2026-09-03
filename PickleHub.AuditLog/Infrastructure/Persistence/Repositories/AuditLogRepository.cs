using Microsoft.EntityFrameworkCore;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;

namespace PickleHub.AuditLog.Infrastructure.Persistence.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AuditLogDbContext _db;

        public AuditLogRepository(AuditLogDbContext db)
        {
            _db = db;
        }

        public async Task<(List<AuditLogs> Items, int TotalItems)> GetPagedAsync(
            Guid? actorId,
            string? actorRole,
            string? action,
            string? entityType,
            Guid? entityId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = _db.AuditLogs.AsNoTracking().AsQueryable();

            if (actorId.HasValue)
                query = query.Where(a => a.ActorId == actorId.Value);

            if (!string.IsNullOrWhiteSpace(actorRole))
                query = query.Where(a => a.ActorRole == actorRole);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action == action);

            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (entityId.HasValue)
                query = query.Where(a => a.EntityId == entityId.Value);

            if (fromDate.HasValue)
            {
                var from = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
                query = query.Where(a => a.OccurredAt >= from);
            }

            if (toDate.HasValue)
            {
                var to = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc);
                query = query.Where(a => a.OccurredAt <= to);
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(a => a.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public void Add(AuditLogs log)
        {
            _db.AuditLogs.Add(log);
        }
    }
}
