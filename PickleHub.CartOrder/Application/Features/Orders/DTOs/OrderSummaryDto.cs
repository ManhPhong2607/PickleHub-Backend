using System;

namespace PickleHub.CartOrder.Application.Features.Orders.DTOs;

public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public Guid? FirstProductId { get; set; }
    public string FirstItemName { get; set; } = string.Empty;
    public string? FirstItemImage { get; set; }
    public List<OrderItemDto>? Items { get; set; }
    public DateTime CreatedAt { get; set; }

    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string? ShippingProvider { get; set; }
    public string? TrackingNumber { get; set; }
}
