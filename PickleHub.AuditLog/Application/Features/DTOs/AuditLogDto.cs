namespace PickleHub.AuditLog.Application.Features.DTOs
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid? ActorId { get; set; }
        public string ActorRole { get; set; } = string.Empty;
        public string ActorEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Metadata { get; set; }
        public DateTime OccurredAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
