using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Domain.Entities;

namespace PickleHub.CartOrder.Application.Common.Interfaces;

public interface ICartOrderDbContext
{
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}