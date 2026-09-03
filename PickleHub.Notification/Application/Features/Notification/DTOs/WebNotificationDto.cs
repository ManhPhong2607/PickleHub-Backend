namespace PickleHub.Notification.Application.Features.Notification.DTOs;

public record WebNotificationDto(
    Guid Id,
    string Title,
    string Content,
    string Type,
    string? DataJson,
    Guid? ReferenceId,
    string Action,
    bool IsRead,
    DateTime CreatedAt
);

public record NotificationListResponse(
    List<WebNotificationDto> Items,
    int TotalCount,
    int UnreadCount,
    int Page,
    int PageSize
);
