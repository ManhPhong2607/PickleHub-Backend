using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Notification.Application.Common.Interfaces;

namespace PickleHub.Notification.Application.Features.Notification.Commands;

public record MarkAllAsReadCommand(Guid UserId) : IRequest<bool>;

public class MarkAllAsReadCommandHandler(INotificationDbContext db)
    : IRequestHandler<MarkAllAsReadCommand, bool>
{
    public async Task<bool> Handle(MarkAllAsReadCommand request, CancellationToken ct)
    {
        var unreadNoti = await db.WebNotifications
            .Where(n => !n.IsRead && n.UserId == request.UserId)
            .ToListAsync(ct);

        if (!unreadNoti.Any())
        {
            return true;
        }

        foreach (var noti in unreadNoti)
        {
            noti.IsRead = true;
        }
        await db.SaveChangesAsync(ct);
        return true;
    }
}
