using Microsoft.EntityFrameworkCore;
using PickleHub.Notification.Domain.Entities;

namespace PickleHub.Notification.Application.Common.Interfaces;

public interface INotificationDbContext
{
    DbSet<WebNotification> WebNotifications { get; }
    DbSet<ProcessedEvent> ProcessedEvents { get; }
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<EmailLog> EmailLogs { get; }
    DbSet<UserNotificationSetting> UserNotificationSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
