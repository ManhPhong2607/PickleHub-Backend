namespace PickleHub.Notification.Domain.Entities;

//Cài d?t nh?n thông báo c?a user
public class UserNotificationSetting
{
    public Guid UserId { get; set; }
    public bool EmailEnabled { get; set; }
    public bool WebEnabled { get; set; }
    public bool OrderNotiEnabled { get; set; }
    public bool PromotionEnabled { get; set; }
    public bool PaymentNotiEnabled { get; set; }
    public bool SystemNotiEnabled { get; set; }
}
