using PickleHub.AuditLog.Domain.Entities;
namespace PickleHub.AuditLog.Domain.Repositories
{
    public interface IAuditLogRepository
    {
        Task<(List<AuditLogs> Items, int TotalItems)> GetPagedAsync(
            Guid? actorId,
            string? actorRole,
            string? action,
            string? entityType,
            Guid? entityId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            CancellationToken ct = default);
        void Add(AuditLogs log);
    }
}
