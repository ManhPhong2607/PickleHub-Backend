using System;
using System.Collections.Generic;

namespace PickleHub.CartOrder.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; } 
    public string? SessionId { get; set; } 
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CartItem> Items { get; set; } = new();
}
