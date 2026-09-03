using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.CartOrder.Application.Common.Interfaces;
using PickleHub.CartOrder.Application.Features.Cart.DTOs;
using PickleHub.CartOrder.Application.Features.Cart.GetCart;

namespace PickleHub.CartOrder.Application.Features.Cart.RemoveItem;

/// <summary>
/// Command xóa 1 dòng sản phẩm trong giỏ theo CartItemId.
/// </summary>
public record RemoveCartItemCommand(
    Guid? UserId,
    Guid CartItemId
) : IRequest<CartDto>;

public class RemoveCartItemCommandHandler(
    ICartOrderDbContext db,
    ISender mediator
) : IRequestHandler<RemoveCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(RemoveCartItemCommand request, CancellationToken ct)
    {
        var item = await db.CartItems
            .Include(x => x.Cart)
            .FirstOrDefaultAsync(x => x.Id == request.CartItemId, ct);

        if (item is null)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm này trong giỏ hàng.");
        }

        var userId = item.Cart.UserId;
        var sessionId = item.Cart.SessionId;

        db.CartItems.Remove(item);
        item.Cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetCartQuery(userId, sessionId), ct);
    }
}
