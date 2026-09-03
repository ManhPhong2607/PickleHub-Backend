using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Application.Features.Cart.DTOs;
using PickleHub.CartOrder.Application.Features.Cart.GetCart;
using PickleHub.CartOrder.Domain.Entities;
using PickleHub.CartOrder.Domain.Interfaces;

namespace PickleHub.CartOrder.Application.Features.Cart.MergeCart;

public record GuestCartItemDto(
    Guid ProductId,
    Guid ProductVariantId,
    int Quantity,
    string? VariantName
);

/// <summary>
/// Command gộp giỏ hàng của Guest (SessionId và/hoặc Danh sách Guest Items) vào tài khoản User sau khi Đăng nhập.
/// </summary>
public record MergeCartCommand(
    Guid UserId,
    string SessionId,
    List<GuestCartItemDto>? GuestItems = null
) : IRequest<CartDto>;

public class MergeCartCommandHandler(
    ICartOrderDbContext db,
    ICatalogClient catalogClient,
    ISender mediator
) : IRequestHandler<MergeCartCommand, CartDto>
{
    public async Task<CartDto> Handle(MergeCartCommand request, CancellationToken ct)
    {
        // 1. Tìm hoặc Tạo mới giỏ hàng của User
        var userCart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (userCart is null)
        {
            userCart = new Domain.Entities.Cart
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Carts.Add(userCart);
        }

        // 2. Thu thập các items cần merge từ CSDL Guest Cart (SessionId) hoặc từ payload GuestItems gửi lên
        var itemsToMerge = new List<(Guid productId, Guid variantId, int qty, string? variantName)>();

        if (!string.IsNullOrEmpty(request.SessionId))
        {
            var guestCart = await db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == request.SessionId, ct);

            if (guestCart is not null && guestCart.Items.Count > 0)
            {
                foreach (var gi in guestCart.Items)
                {
                    var vId = gi.ProductVariantId != Guid.Empty ? gi.ProductVariantId : gi.ProductId;
                    itemsToMerge.Add((gi.ProductId, vId, gi.Quantity, gi.VariantAttributesSnapshot));
                }
                db.Carts.Remove(guestCart);
            }
        }

        if (request.GuestItems is not null && request.GuestItems.Count > 0)
        {
            foreach (var item in request.GuestItems)
            {
                var vId = item.ProductVariantId != Guid.Empty ? item.ProductVariantId : item.ProductId;
                if (!itemsToMerge.Any(x => x.productId == item.ProductId && x.variantId == vId))
                {
                    itemsToMerge.Add((item.ProductId, vId, item.Quantity, item.VariantName));
                }
            }
        }

        // 3. Tiến hành Validate và Merge từng item vào User Cart
        foreach (var (productId, variantId, qty, variantName) in itemsToMerge)
        {
            if (qty <= 0) continue;

            // Validate sản phẩm từ Catalog DB (nếu có)
            var productDetails = await catalogClient.GetProductDetailsAsync(productId, ct);
            
            var variant = productDetails?.Variants.FirstOrDefault(v => v.Id == variantId || v.Id == productId);
            var freshPrice = variant?.Price ?? productDetails?.BasePrice ?? 0;
            var imageUrl = productDetails?.Images
                .Where(img => !img.IsSizeChart)
                .OrderBy(img => img.SortOrder)
                .FirstOrDefault()?.Url ?? "/images/paddle.png";
            var productName = productDetails?.Name ?? "Sản phẩm";

            // So khớp chính xác theo CẢ ProductId VÀ ProductVariantId
            var existingUserItem = userCart.Items
                .FirstOrDefault(i => i.ProductId == productId && i.ProductVariantId == variantId);

            if (existingUserItem is not null)
            {
                existingUserItem.Quantity += qty;
                existingUserItem.UnitPrice = freshPrice; // Cập nhật giá tươi mới nhất từ DB
                existingUserItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                userCart.Items.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = userCart.Id,
                    ProductId = productId,
                    ProductVariantId = variantId,
                    ProductNameSnapshot = productName,
                    VariantAttributesSnapshot = variantName ?? string.Empty,
                    ImageUrlSnapshot = imageUrl,
                    UnitPrice = freshPrice,
                    Quantity = qty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Logging or graceful fallback if items conflict
            Console.WriteLine($"[MergeCart] SaveChanges warning: {ex.Message}");
        }

        // Trả về chi tiết giỏ hàng của User sau khi đã merge
        return await mediator.Send(new GetCartQuery(request.UserId), ct);
    }
}
