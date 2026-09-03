using PickleHub.Common.Enums;
using System;
using System.Collections.Generic;

namespace PickleHub.Common.Events.Order;

public record OrderCancelledEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public OrderStatus PreviousStatus { get; init; } 
    public bool IsStockReserved { get; init; }
    public List<OrderItemPayload> Items { get; init; } = new(); // để Inventory hoàn kho
    public string CancelledBy { get; init; } = string.Empty;   // "Customer" | "Admin" | "System"
    public string? CancelReason { get; init; }
    public DateTime CancelledAt { get; init; }
}
