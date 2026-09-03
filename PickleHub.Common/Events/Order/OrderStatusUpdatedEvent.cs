using System;
using System.Collections.Generic;
using PickleHub.Common.Enums;

namespace PickleHub.Common.Events.Order;

public record OrderStatusUpdatedEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public OrderStatus OldStatus { get; init; } 
    public OrderStatus NewStatus { get; init; } 
    public List<OrderItemPayload> Items { get; init; } = new();

    // Chỉ có giá trị khi NewStatus = Shipping
    public string? ShippingProvider { get; init; }   // "GHTK" | "GHN" | "ViettelPost"
    public string? TrackingNumber { get; init; }
    public string? TrackingUrl { get; init; }
    public DateTime UpdatedAt { get; init; }
}
