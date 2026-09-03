namespace PickleHub.Notification.Domain.Entities;

//Ch?ng trùng l?p event
public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; }
    public string? ConsumerName { get; set; }
    public DateTime ProcessedAt { get; set; }
}
