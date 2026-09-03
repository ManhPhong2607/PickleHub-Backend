using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Notification.Application.Common.Interfaces;

namespace PickleHub.Notification.Application.Features.Notification.Commands;
//Ðánh d?u 1 thông báo c? th? là dã d?c
public record MarkAsReadCommand(Guid NotificationId, Guid UserId) : IRequest<bool>;

public class MarkAsReadCommandHandler(INotificationDbContext db) : IRequestHandler<MarkAsReadCommand, bool>
{
    public async Task<bool> Handle(MarkAsReadCommand request, CancellationToken ct)
    {
        var noti = await db.WebNotifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == request.UserId);
        if (noti is null)
        {
            return false;
        }

        noti.IsRead = true;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
