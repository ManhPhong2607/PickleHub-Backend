using System;

namespace PickleHub.CartOrder.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    
    public Guid ProductVariantId { get; set; }
    public Guid ProductId { get; set; }
    
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string VariantAttributesSnapshot { get; set; } = string.Empty;
    public string? ImageUrlSnapshot { get; set; }
    
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}
