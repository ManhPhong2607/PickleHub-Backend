using PickleHub.Notification.Domain.Enums;

namespace PickleHub.Notification.Domain.Entities;

//Luu thông báo in-app cho khách
public class WebNotification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; }  = string.Empty;
    public NotificationType Type { get; set; }
    public string? DataJson { get; set; }
    public Guid? ReferenceId { get; set; }
    public string Action { get; set; } = "VIEW_ORDER";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
