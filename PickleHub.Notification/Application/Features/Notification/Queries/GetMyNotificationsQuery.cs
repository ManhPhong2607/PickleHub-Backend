using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Notification.Application.Common.Interfaces;
using PickleHub.Notification.Application.Features.Notification.DTOs;
namespace PickleHub.Notification.Application.Features.Notification.Queries;

public record GetMyNotificationsQuery(Guid UserId, int Page = 1, int PageSize = 10, bool? IsRead = null, bool IsAdmin = false)
    : IRequest<NotificationListResponse>;

public class GetMyNotificationsQueryHandler(INotificationDbContext db)
    : IRequestHandler<GetMyNotificationsQuery, NotificationListResponse>
{
    public async Task<NotificationListResponse> Handle(GetMyNotificationsQuery request, CancellationToken ct)
    {
        var query = db.WebNotifications.AsNoTracking()
            .Where(n => n.UserId == request.UserId
                     || (request.IsAdmin && n.UserId == Guid.Empty));

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        var totalCount = await query.CountAsync(ct);
        var unreadCount = await db.WebNotifications.AsNoTracking()
            .CountAsync(n => (n.UserId == request.UserId
                           || (request.IsAdmin && n.UserId == Guid.Empty))
                          && !n.IsRead, ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new WebNotificationDto(n.Id, n.Title, n.Content, n.Type.ToString(), n.DataJson, n.ReferenceId, n.Action, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);

        return new NotificationListResponse(items, totalCount, unreadCount, request.Page, request.PageSize);
    }
}