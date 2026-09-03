using System;
using System.Collections.Generic;
using PickleHub.Common.Enums;

namespace PickleHub.CartOrder.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; } 
    
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    public string PaymentMethod { get; set; } = "COD"; 
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid; 
    public bool IsStockReserved { get; set; } = false; 
    
    // Địa chỉ giao hàng chi tiết dạng Snapshot
    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingProvince { get; set; } = string.Empty;
    public string ShippingDistrict { get; set; } = string.Empty;
    public string ShippingWard { get; set; } = string.Empty;
    public string ShippingStreetAddress { get; set; } = string.Empty;
    
    // Đơn vị vận chuyển và Mã tra cứu
    public string? ShippingProvider { get; set; }
    public string? TrackingNumber { get; set; }
    public string? TrackingUrl { get; set; }
    
    // Giá trị đơn hàng và Ghi chú
    public decimal Subtotal { get; set; }
    public decimal LoyaltyDiscountPercent { get; set; }
    public decimal LoyaltyDiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    
    // Thông tin hủy đơn
    public string? CancelledBy { get; set; }
    public string? CancelReason { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
}
