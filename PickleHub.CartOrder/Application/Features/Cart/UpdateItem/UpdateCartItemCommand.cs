using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Application.Features.Cart.DTOs;
using PickleHub.CartOrder.Application.Features.Cart.GetCart;

namespace PickleHub.CartOrder.Application.Features.Cart.UpdateItem;

/// <summary>
/// Command cập nhật số lượng dòng CartItem theo itemId (Nếu quantity <= 0 thì xóa khỏi giỏ).
/// </summary>
public record UpdateCartItemCommand(
    Guid? UserId,
    Guid CartItemId,
    int Quantity
) : IRequest<CartDto>;

public class UpdateCartItemCommandHandler(
    ICartOrderDbContext db,
    ISender mediator
) : IRequestHandler<UpdateCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(UpdateCartItemCommand request, CancellationToken ct)
    {
        var item = await db.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == request.CartItemId, ct);

        if (item is null)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm này trong giỏ hàng.");
        }

        if (request.Quantity <= 0)
        {
            db.CartItems.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
            item.UpdatedAt = DateTime.UtcNow;
        }

        item.Cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetCartQuery(item.Cart.UserId, item.Cart.SessionId), ct);
    }
}
