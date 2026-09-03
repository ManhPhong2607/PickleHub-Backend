using PickleHub.Notification.Domain.Enums;

namespace PickleHub.Notification.Domain.Entities;

// Nh?t ký & Hàng ch? g?i email
public class EmailLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? EventId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; }  = string.Empty;
    public string BodyHtml { get; set; }   = string.Empty;
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
