using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.CartOrder.Application.Features.Orders.AdminCommands;
using PickleHub.CartOrder.Application.Features.Orders.CancelOrder;
using PickleHub.CartOrder.Application.Features.Orders.Checkout;
using PickleHub.CartOrder.Application.Features.Orders.DTOs;
using PickleHub.CartOrder.Application.Features.Orders.GetDashboardSummary;
using PickleHub.CartOrder.Application.Features.Orders.GetMyOrders;
using PickleHub.CartOrder.Application.Features.Orders.GetMyOrdersSummary;
using PickleHub.CartOrder.Application.Features.Orders.GetOrderById;
using PickleHub.CartOrder.Application.Features.Orders.GetOrderByIdAdmin;
using PickleHub.CartOrder.Application.Features.Orders.GetOrdersAdmin;
using PickleHub.Common.Enums;
using PickleHub.Common.DTOs;

using Microsoft.Extensions.Configuration;

namespace PickleHub.CartOrder.Controllers;

[ApiController]
[Authorize]
public class OrderController(ISender mediator, IConfiguration config) : ControllerBase
{
    // CUSTOMER ORDER ENDPOINTS

    // POST /orders -> Tạo đơn hàng (Checkout)
    [HttpPost("orders")]
    public async Task<ActionResult<CheckoutResponse>> Checkout(
        [FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await mediator.Send(new CheckoutCommand(
            userId,
            request.AddressId,
            request.PaymentMethod,
            request.Note
        ), ct);
        
        return CreatedAtAction(nameof(GetMyOrderById), new { orderId = result.OrderId }, result);
    }

    // GET /orders/me -> Xem danh sách đơn hàng cá nhân
    [HttpGet("orders/me")]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetMyOrders(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await mediator.Send(new GetMyOrdersQuery(userId), ct);
        return Ok(result);
    }

    // GET /orders/me/summary -> Xem badge count theo trạng thái (Customer Panel)
    [HttpGet("orders/me/summary")]
    public async Task<ActionResult<OrderBadgeSummaryDto>> GetMyOrdersSummary(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await mediator.Send(new GetMyOrdersSummaryQuery(userId), ct);
        return Ok(result);
    }

    // GET /orders/me/{orderId} -> Xem chi tiết đơn hàng cá nhân
    [HttpGet("orders/me/{orderId:guid}")]
    public async Task<ActionResult<OrderDto>> GetMyOrderById(Guid orderId, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await mediator.Send(new GetOrderByIdQuery(orderId, userId), ct);
        return Ok(result);
    }

    // PATCH /orders/me/{orderId}/cancel -> Khách hàng tự hủy đơn
    [HttpPatch("orders/me/{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelMyOrder(
        Guid orderId, [FromBody] CancelOrderRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        await mediator.Send(new CancelOrderCommand(orderId, userId, request.Reason), ct);
        return NoContent();
    }
    
    // ADMIN ORDER ENDPOINTS (Khóa quyền Admin)

    // GET /admin/orders -> Danh sách đơn hàng cho Admin (Phân trang + Lọc)
    [HttpGet("admin/orders")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetOrdersAdmin(
        [FromQuery] OrderStatus? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetOrdersAdminQuery(status, keyword, page, pageSize), ct);
        return Ok(result);
    }

    // GET /admin/orders/{orderId} -> Chi tiết đơn hàng cho Admin
    [HttpGet("admin/orders/{orderId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> GetOrderByIdAdmin(Guid orderId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrderByIdAdminQuery(orderId), ct);
        return Ok(result);
    }

    // PATCH /admin/orders/{orderId}/confirm -> Admin xác nhận đơn hàng
    [HttpPatch("admin/orders/{orderId:guid}/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConfirmOrder(Guid orderId, CancellationToken ct)
    {
        await mediator.Send(new ConfirmOrderCommand(orderId), ct);
        return NoContent();
    }

    // PATCH /admin/orders/{orderId}/shipping -> Admin cập nhật thông tin vận chuyển
    [HttpPatch("admin/orders/{orderId:guid}/shipping")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateShipping(
        Guid orderId, [FromBody] UpdateShippingRequest request, CancellationToken ct)
    {
        await mediator.Send(new UpdateShippingCommand(orderId, request.ShippingProvider, request.TrackingNumber), ct);
        return Ok();
    }

    // PATCH /admin/orders/{orderId}/complete -> Admin xác nhận giao hàng hoàn thành
    [HttpPatch("admin/orders/{orderId:guid}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteOrder(Guid orderId, CancellationToken ct)
    {
        await mediator.Send(new CompleteOrderCommand(orderId), ct);
        return NoContent();
    }

    // PATCH /admin/orders/{orderId}/cancel -> Admin hủy đơn hàng
    [HttpPatch("admin/orders/{orderId:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminCancelOrder(
        Guid orderId, [FromBody] CancelOrderRequest request, CancellationToken ct)
    {
        await mediator.Send(new AdminCancelOrderCommand(orderId, request.Reason), ct);
        return NoContent();
    }

    // GET /internal/orders/{orderId} -> Endpoint nội bộ cho Payment Service gọi check số tiền
    [HttpGet("internal/orders/{orderId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<OrderDto>> GetOrderInternal(Guid orderId, CancellationToken ct)
    {
        // Kiểm tra header bảo mật nội bộ sử dụng mật khóa cấu hình
        var internalToken = config["Security:InternalApiKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
        if (!Request.Headers.TryGetValue("X-Internal-Key", out var headerKey) || headerKey != internalToken)
        {
            return Unauthorized("Yêu cầu này không hợp lệ hoặc thiếu mã khóa dịch vụ nội bộ.");
        }

        var result = await mediator.Send(new GetOrderByIdAdminQuery(orderId), ct);
        return Ok(result);
    }

    // GET /orders/{orderId}/verify -> Endpoint cho Review Service gọi kiểm tra xem đơn hàng đã hoàn tất/thanh toán và chứa sản phẩm
    [HttpGet("orders/{orderId:guid}/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOrderForReview(
        Guid orderId,
        [FromQuery] Guid userId,
        [FromQuery] Guid productId,
        CancellationToken ct)
    {
        var result = await mediator.Send(new PickleHub.CartOrder.Application.Features.Orders.VerifyOrder.VerifyOrderQuery(orderId, userId, productId), ct);
        return Ok(result);
    }

    // GET /admin/orders/dashboard/summary -> Thống kê Dashboard cho Admin
    [HttpGet("admin/orders/dashboard/summary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDashboardSummaryDto>> GetDashboardSummary(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDashboardSummaryQuery(), ct);
        return Ok(result);
    }

    // GET /admin/orders/dashboard/revenue-analytics -> Biểu đồ Doanh thu theo thời gian
    [HttpGet("admin/orders/dashboard/revenue-analytics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PickleHub.CartOrder.Application.Features.Orders.GetRevenueAnalytics.RevenueAnalyticsResultDto>> GetRevenueAnalytics(
        [FromQuery] int? days = 30,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new PickleHub.CartOrder.Application.Features.Orders.GetRevenueAnalytics.GetRevenueAnalyticsQuery(days, startDate, endDate), ct);
        return Ok(result);
    }

    // GET /admin/orders/dashboard/status-distribution -> Biểu đồ Đơn hàng theo trạng thái
    [HttpGet("admin/orders/dashboard/status-distribution")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PickleHub.CartOrder.Application.Features.Orders.GetOrderStatusDistribution.OrderStatusDistributionResultDto>> GetOrderStatusDistribution(
        [FromQuery] int? days = 30,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new PickleHub.CartOrder.Application.Features.Orders.GetOrderStatusDistribution.GetOrderStatusDistributionQuery(days, startDate, endDate), ct);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng trong Token xác thực.");
    }
}

public record CheckoutRequest(
    Guid AddressId,
    string PaymentMethod = "COD",
    string? Note = null
);

public record CancelOrderRequest(string? Reason = null);

public record UpdateShippingRequest(
    string ShippingProvider,
    string TrackingNumber
);
