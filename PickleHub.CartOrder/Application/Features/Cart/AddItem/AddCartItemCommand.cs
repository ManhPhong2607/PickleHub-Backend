using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Application.Features.Cart.DTOs;
using PickleHub.CartOrder.Application.Features.Cart.GetCart;
using PickleHub.CartOrder.Domain.Entities;
using PickleHub.CartOrder.Domain.Interfaces;

namespace PickleHub.CartOrder.Application.Features.Cart.AddItem;

// Command thêm sản phẩm vào giỏ hàng (Hỗ trợ cả User và Guest via SessionId).
public record AddCartItemCommand(
    Guid? UserId,
    Guid ProductVariantId,
    int Quantity,
    string? SessionId = null
) : IRequest<CartDto>;

public class AddCartItemHandler(
    ICartOrderDbContext db,
    ICatalogClient catalogClient,
    ISender mediator
) : IRequestHandler<AddCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(AddCartItemCommand request, CancellationToken ct)
    {
        if (request.Quantity < 1)
        {
            throw new ArgumentException("Số lượng sản phẩm thêm vào giỏ phải lớn hơn hoặc bằng 1.");
        }

        // Lấy chi tiết sản phẩm từ Catalog Service
        var productDetails = await catalogClient.GetProductDetailsAsync(request.ProductVariantId, ct);
        if (productDetails is null)
        {
            throw new KeyNotFoundException($"Sản phẩm/Biến thể với ID {request.ProductVariantId} không tồn tại.");
        }

        var variants = productDetails.Variants ?? new List<CatalogProductVariantDto>();
        var images = productDetails.Images ?? new List<CatalogProductImageDto>();

        var variant = variants.FirstOrDefault(v => v.Id == request.ProductVariantId);
        var unitPrice = variant?.Price ?? productDetails.BasePrice;
        var imageUrl = images
            .Where(img => !img.IsSizeChart)
            .OrderBy(img => img.SortOrder)
            .FirstOrDefault()?.Url ?? string.Empty;

        // Tìm hoặc tạo mới Cart
        var cart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => 
                (request.UserId.HasValue && c.UserId == request.UserId.Value) || 
                (!string.IsNullOrEmpty(request.SessionId) && c.SessionId == request.SessionId), ct);

        if (cart is null)
        {
            cart = new Domain.Entities.Cart
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                SessionId = request.SessionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Carts.Add(cart);
        }

        // Kiểm tra xem sản phẩm đã có trong giỏ chưa để cộng dồn hoặc thêm mới
        var existingItem = cart.Items
            .FirstOrDefault(i => i.ProductVariantId == request.ProductVariantId || i.ProductId == request.ProductVariantId);

        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.UnitPrice = unitPrice;
            existingItem.ImageUrlSnapshot = imageUrl;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existingItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductVariantId = request.ProductVariantId,
                ProductId = productDetails.Id,
                ProductNameSnapshot = productDetails.Name,
                VariantAttributesSnapshot = variant?.Sku ?? string.Empty,
                ImageUrlSnapshot = imageUrl,
                UnitPrice = unitPrice,
                Quantity = request.Quantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.CartItems.Add(existingItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetCartQuery(request.UserId, request.SessionId), ct);
    }
}
