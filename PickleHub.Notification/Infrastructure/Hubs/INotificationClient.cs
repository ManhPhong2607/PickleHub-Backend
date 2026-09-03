namespace PickleHub.Notification.Infrastructure.Hubs;

public interface INotificationClient
{
    // Bắn thông báo Web Notification mới xuống Client trình duyệt.
    Task ReceiveNotification(object notification);
    // Cập nhật số đếm thông báo chưa đọc (Badge count trên Bell Icon).
    Task ReceiveUnreadCount(int unreadCount);
}
