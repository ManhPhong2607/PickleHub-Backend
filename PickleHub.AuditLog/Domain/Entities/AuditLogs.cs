using PickleHub.Common.Domain;

namespace PickleHub.AuditLog.Domain.Entities
{
    public class AuditLogs : BaseEntity
    {
        public Guid? ActorId { get; private set; }
        public string ActorRole { get; private set; } = string.Empty;
        public string ActorEmail { get; private set; } = string.Empty;
        public string Action { get; private set; } = string.Empty;
        public string EntityType { get; private set; } = string.Empty;
        public Guid? EntityId { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public string? Metadata { get; private set; }
        public DateTime OccurredAt { get; private set; }

        private AuditLogs() { }

        public static AuditLogs Create(
            string action,
            string entityType,
            Guid? entityId,
            string description,
            DateTime occurredAt,
            Guid? actorId = null,
            string actorRole = "System",
            string actorEmail = "system",
            string? metadata = null)
        {
            return new AuditLogs
            {
                ActorId = actorId,
                ActorRole = actorRole,
                ActorEmail = actorEmail,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                Metadata = metadata,
                OccurredAt = occurredAt
            };
        }
    }
}
