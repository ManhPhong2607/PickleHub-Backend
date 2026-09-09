namespace PickleHub.Common.Events.Order;

public record OrderCheckoutFailedEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public List<ReservedItemPayload> Items { get; init; } = new();
    public string Reason { get; init; } = string.Empty;
    public DateTime OccurAt { get; init; } = DateTime.UtcNow;
    
}

public record ReservedItemPayload
{
    public Guid ProductVariantId { get; init; }
    public int Quantity { get; init; }
}