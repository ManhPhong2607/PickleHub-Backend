using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.CartOrder.Application.Features.Cart.AddItem;
using PickleHub.CartOrder.Application.Features.Cart.DTOs;
using PickleHub.CartOrder.Application.Features.Cart.GetCart;
using PickleHub.CartOrder.Application.Features.Cart.MergeCart;
using PickleHub.CartOrder.Application.Features.Cart.RemoveItem;
using PickleHub.CartOrder.Application.Features.Cart.UpdateItem;

namespace PickleHub.CartOrder.Controllers;

[ApiController]
[Route("cart")]
public class CartController(ISender mediator) : ControllerBase
{
    // GET /cart -> Xem giỏ hàng (Public hoặc Authenticated)
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart([FromQuery] string? sessionId, CancellationToken ct)
    {
        var userId = TryGetUserId();
        var result = await mediator.Send(new GetCartQuery(userId, sessionId), ct);
        return Ok(result);
    }

    // POST /cart/items -> Thêm sản phẩm vào giỏ
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var userId = TryGetUserId();
        var result = await mediator.Send(new AddCartItemCommand(userId, request.ProductVariantId, request.Quantity, request.SessionId), ct);
        return Ok(result);
    }

    // PUT /cart/items/{itemId} -> Cập nhật số lượng sản phẩm trong giỏ
    [HttpPut("items/{itemId:guid}")]
    public async Task<ActionResult<CartDto>> UpdateItem(
        Guid itemId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var userId = TryGetUserId();
        var result = await mediator.Send(new UpdateCartItemCommand(userId, itemId, request.Quantity), ct);
        return Ok(result);
    }

    // DELETE /cart/items/{itemId} -> Xóa 1 item khỏi giỏ
    [HttpDelete("items/{itemId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid itemId, CancellationToken ct)
    {
        var userId = TryGetUserId();
        var result = await mediator.Send(new RemoveCartItemCommand(userId, itemId), ct);
        return Ok(result);
    }

    // POST /cart/merge -> Merge giỏ hàng Guest vào Account khi đăng nhập
    [HttpPost("merge")]
    [Authorize]
    public async Task<ActionResult<CartDto>> MergeCart([FromBody] MergeCartRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await mediator.Send(new MergeCartCommand(userId, request.SessionId), ct);
        return Ok(result);
    }
    
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin định danh người dùng trong Token.");
        }
        return userId;
    }

    private Guid? TryGetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

public record AddCartItemRequest(Guid ProductVariantId, int Quantity, string? SessionId = null);
public record UpdateCartItemRequest(int Quantity);
public record MergeCartRequest(string SessionId);
