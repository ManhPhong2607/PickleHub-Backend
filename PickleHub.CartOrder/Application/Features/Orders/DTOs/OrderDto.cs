using System;
using System.Collections.Generic;

namespace PickleHub.CartOrder.Application.Features.Orders.DTOs;

public record OrderDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    
    public string Status { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    
    public string ShippingFullName { get; init; } = string.Empty;
    public string ShippingPhone { get; init; } = string.Empty;
    public string ShippingProvince { get; init; } = string.Empty;
    public string ShippingDistrict { get; init; } = string.Empty;
    public string ShippingWard { get; init; } = string.Empty;
    public string ShippingStreetAddress { get; init; } = string.Empty;
    
    public string? ShippingProvider { get; init; }
    public string? TrackingNumber { get; init; }
    public string? TrackingUrl { get; init; }
    
    public List<OrderItemDto> Items { get; init; } = [];
    
    public decimal Subtotal { get; init; }
    public decimal LoyaltyDiscountPercent { get; init; }
    public decimal LoyaltyDiscountAmount { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal TotalAmount { get; init; }
    
    public string? CancelledBy { get; init; }
    public string? CancelReason { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
