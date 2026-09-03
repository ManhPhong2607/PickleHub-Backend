using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace PickleHub.Notification.Infrastructure.Hubs;

[Authorize]
public class NotificationHub(ILogger<NotificationHub> logger) : Hub<INotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            logger.LogInformation("Client [{ConnectionId}] kết nối SignalR Hub cho User [{UserId}]", Context.ConnectionId, userId);
        }

        // Admin join thêm group "Admins" để nhận các alert hệ thống (low stock, v.v.)
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            logger.LogInformation("Client [{ConnectionId}] đã join group Admins", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            var userGroup = $"User_{userId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userGroup);
            logger.LogInformation("Client WebSocket [{ConnectionId}] ngắt kết nối SignalR Hub cho User [{UserId}]", Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
