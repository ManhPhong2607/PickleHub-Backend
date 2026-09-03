using System.Collections.Generic;
using System.Linq;

namespace PickleHub.CartOrder.Application.Features.Cart.DTOs;

public record CartDto
{
    public List<CartItemDto> Items { get; init; } = [];
    public decimal TotalAmount => Items.Sum(i => i.Subtotal);
    public int ItemCount => Items.Sum(i => i.Quantity);
}
