namespace PickleHub.Notification.Domain.Entities;

//Template HTML d?ng 
public class NotificationTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Subject { get; set; }
    public string BodyHtml { get; set; }
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}
