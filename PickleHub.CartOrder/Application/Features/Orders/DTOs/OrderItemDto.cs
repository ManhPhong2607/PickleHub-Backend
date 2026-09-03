using System;

namespace PickleHub.CartOrder.Application.Features.Orders.DTOs;

public record OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductVariantId { get; init; }
    public Guid ProductId { get; init; }
    
    public string ProductNameSnapshot { get; init; } = string.Empty;
    public string VariantAttributesSnapshot { get; init; } = string.Empty;
    public string? ImageUrlSnapshot { get; init; }
    
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
}
