using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Application.Features.Cart.DTOs;
using PickleHub.CartOrder.Application.Features.Cart.GetCart;
using PickleHub.CartOrder.Domain.Entities;

namespace PickleHub.CartOrder.Application.Features.Cart.MergeCart;

/// <summary>
/// Command gộp giỏ hàng của Guest (SessionId) vào tài khoản User sau khi Đăng nhập.
/// </summary>
public record MergeCartCommand(
    Guid UserId,
    string SessionId
) : IRequest<CartDto>;

public class MergeCartCommandHandler(
    ICartOrderDbContext db,
    ISender mediator
) : IRequestHandler<MergeCartCommand, CartDto>
{
    public async Task<CartDto> Handle(MergeCartCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.SessionId))
        {
            return await mediator.Send(new GetCartQuery(request.UserId), ct);
        }

        // Tìm giỏ hàng Guest theo SessionId
        var guestCart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == request.SessionId, ct);

        if (guestCart is null || guestCart.Items.Count == 0)
        {
            return await mediator.Send(new GetCartQuery(request.UserId), ct);
        }

        // Tìm hoặc Tạo mới giỏ hàng của User
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

        // Gộp sản phẩm từ Guest Cart sang User Cart
        foreach (var guestItem in guestCart.Items)
        {
            var existingUserItem = userCart.Items
                .FirstOrDefault(i => i.ProductVariantId == guestItem.ProductVariantId || i.ProductId == guestItem.ProductId);

            if (existingUserItem is not null)
            {
                existingUserItem.Quantity += guestItem.Quantity;
                existingUserItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                userCart.Items.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = userCart.Id,
                    ProductVariantId = guestItem.ProductVariantId,
                    ProductId = guestItem.ProductId,
                    ProductNameSnapshot = guestItem.ProductNameSnapshot,
                    VariantAttributesSnapshot = guestItem.VariantAttributesSnapshot,
                    ImageUrlSnapshot = guestItem.ImageUrlSnapshot,
                    UnitPrice = guestItem.UnitPrice,
                    Quantity = guestItem.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // Xóa giỏ hàng Guest sau khi đã gộp thành công
        db.Carts.Remove(guestCart);
        await db.SaveChangesAsync(ct);

        // Trả về chi tiết giỏ hàng sau khi gộp
        return await mediator.Send(new GetCartQuery(request.UserId), ct);
    }
}
