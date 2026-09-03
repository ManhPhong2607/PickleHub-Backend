using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Application.Features.Cart.DTOs;
using PickleHub.CartOrder.Domain.Interfaces;

namespace PickleHub.CartOrder.Application.Features.Cart.GetCart;

// Query xem chi tiết giỏ hàng (Hỗ trợ cả User đã đăng nhập và Guest qua SessionId).
public record GetCartQuery(Guid? UserId, string? SessionId = null) : IRequest<CartDto>;

public class GetCartHandler(ICartOrderDbContext db, ICatalogClient catalogClient) : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken ct)
    {
        if (!request.UserId.HasValue && string.IsNullOrEmpty(request.SessionId))
        {
            return new CartDto();
        }

        var cart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => 
                (request.UserId.HasValue && c.UserId == request.UserId.Value) || 
                (!string.IsNullOrEmpty(request.SessionId) && c.SessionId == request.SessionId), ct);

        if (cart is null)
        {
            return new CartDto();
        }

        var cartItemDtos = new List<CartItemDto>();

        foreach (var item in cart.Items)
        {
            // Gọi Catalog Service để lấy chi tiết tươi mới
            var productDetails = await catalogClient.GetProductDetailsAsync(item.ProductId, ct);
            
            var variant = productDetails?.Variants.FirstOrDefault(v => v.Id == item.ProductVariantId || v.Id == item.ProductId);
            var unitPrice = variant?.Price ?? productDetails?.BasePrice ?? item.UnitPrice;

            var imageUrl = productDetails?.Images
                .Where(img => !img.IsSizeChart)
                .OrderBy(img => img.SortOrder)
                .FirstOrDefault()?.Url ?? item.ImageUrlSnapshot;

            cartItemDtos.Add(new CartItemDto
            {
                Id = item.Id,
                ProductVariantId = item.ProductVariantId != Guid.Empty ? item.ProductVariantId : item.ProductId,
                ProductId = item.ProductId,
                ProductNameSnapshot = !string.IsNullOrEmpty(item.ProductNameSnapshot) ? item.ProductNameSnapshot : (productDetails?.Name ?? string.Empty),
                VariantAttributesSnapshot = item.VariantAttributesSnapshot,
                ImageUrlSnapshot = imageUrl,
                UnitPrice = unitPrice,
                Quantity = item.Quantity
            });
        }
        
        return new CartDto
        {
            Items = cartItemDtos
        };
    }
}
