using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Notification.Application.Features.Notification.Commands;
using PickleHub.Notification.Application.Features.Notification.DTOs;
using PickleHub.Notification.Application.Features.Notification.Queries;

namespace PickleHub.Notification.Controllers;

[ApiController]
[Route("notifications")]
[Authorize]
public class NotificationController(ISender mediator) : ControllerBase
{
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

    /// <summary>
    /// Lấy danh sách thông báo cá nhân (phân trang + số đếm unreadCount).
    /// GET /notifications/me?page=1&pageSize=10&isRead=false
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<NotificationListResponse>> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isRead = null,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var query = new GetMyNotificationsQuery(userId, page, pageSize, isRead, isAdmin);
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Đánh dấu 1 thông báo cụ thể là đã đọc.
    /// PUT /notifications/{id}/read
    /// </summary>
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var success = await mediator.Send(new MarkAsReadCommand(id, userId), ct);
        
        if (!success)
        {
            return NotFound(new { message = "Không tìm thấy thông báo hoặc bạn không có quyền cập nhật." });
        }

        return Ok(new { success = true, message = "Đã đánh dấu thông báo là đã đọc." });
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo của tôi là đã đọc.
    /// PUT /notifications/read-all
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct = default)
    {
        var userId = GetUserId();
        await mediator.Send(new MarkAllAsReadCommand(userId), ct);
        return Ok(new { success = true, message = "Đã đánh dấu tất cả thông báo là đã đọc." });
    }
}
