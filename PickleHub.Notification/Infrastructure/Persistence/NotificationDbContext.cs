using Microsoft.EntityFrameworkCore;
using PickleHub.Notification.Application.Common.Interfaces;
using PickleHub.Notification.Domain.Entities;

namespace PickleHub.Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext, INotificationDbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options){}

    public DbSet<WebNotification> WebNotifications => Set<WebNotification>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<UserNotificationSetting> UserNotificationSettings => Set<UserNotificationSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. WebNotification Configuration
        modelBuilder.Entity<WebNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(100).HasDefaultValue("VIEW_ORDER");
            entity.HasIndex(e => new { e.UserId, e.IsRead }).HasDatabaseName("idx_web_notifications_user_read");
        });

        // 2. ProcessedEvent Configuration (Idempotency Guard)
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(150);
            entity.Property(e => e.ConsumerName).HasMaxLength(150);
        });

        // 3. NotificationTemplate Configuration
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(250);
            entity.Property(e => e.BodyHtml).IsRequired();
        });

        // 4. EmailLog Configuration
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ToEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(250);
            entity.HasIndex(e => new { e.ToEmail, e.Status }).HasDatabaseName("idx_email_logs_to_status");
        });

        // 5. UserNotificationSetting Configuration
        modelBuilder.Entity<UserNotificationSetting>(entity =>
        {
            entity.HasKey(e => e.UserId);
        });

        // 6. Seed Data cho NotificationTemplates
        SeedNotificationTemplates(modelBuilder);
    }

    private static void SeedNotificationTemplates(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<NotificationTemplate>().HasData(
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "OrderConfirmation",
                Subject = "[PickleHub] Xác nhận đơn hàng #{{OrderCode}}",
                BodyHtml = @"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;"">
    <div style=""background-color: #1a56db; color: #ffffff; padding: 20px; text-align: center;"">
        <h1 style=""margin: 0; font-size: 24px;"">PickleHub Store</h1>
        <p style=""margin: 5px 0 0 0; font-size: 14px;"">Xác nhận đơn hàng thành công</p>
    </div>
    <div style=""padding: 20px;"">
        <p>Xin chào <strong>{{CustomerName}}</strong>,</p>
        <p>Cảm ơn bạn đã đặt hàng tại PickleHub! Đơn hàng <strong>#{{OrderCode}}</strong> của bạn đã được ghi nhận và đang chờ xử lý.</p>
        
        <table style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
            <tr style=""background-color: #f8f9fa;"">
                <th style=""padding: 10px; border: 1px solid #dee2e6; text-align: left;"">Mã đơn hàng</th>
                <td style=""padding: 10px; border: 1px solid #dee2e6;"">#{{OrderCode}}</td>
            </tr>
            <tr>
                <th style=""padding: 10px; border: 1px solid #dee2e6; text-align: left;"">Tổng giá trị</th>
                <td style=""padding: 10px; border: 1px solid #dee2e6; color: #1a56db; font-weight: bold;"">{{TotalAmount}} VNĐ</td>
            </tr>
            <tr style=""background-color: #f8f9fa;"">
                <th style=""padding: 10px; border: 1px solid #dee2e6; text-align: left;"">Địa chỉ giao hàng</th>
                <td style=""padding: 10px; border: 1px solid #dee2e6;"">{{ShippingAddress}}</td>
            </tr>
        </table>
        
        <p>Chúng tôi sẽ thông báo cho bạn ngay khi đơn hàng bắt đầu được vận chuyển.</p>
    </div>
    <div style=""background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d;"">
        &copy; 2026 PickleHub Store. Mọi quyền được bảo lưu.
    </div>
</div>",
                Version = 1,
                UpdatedAt = now
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "PaymentSuccess",
                Subject = "[PickleHub] Xác nhận thanh toán đơn hàng #{{OrderCode}} thành công",
                BodyHtml = @"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;"">
    <div style=""background-color: #16a34a; color: #ffffff; padding: 20px; text-align: center;"">
        <h1 style=""margin: 0; font-size: 24px;"">PickleHub Store</h1>
        <p style=""margin: 5px 0 0 0; font-size: 14px;"">Xác nhận thanh toán thành công</p>
    </div>
    <div style=""padding: 20px;"">
        <p>Xin chào <strong>{{CustomerName}}</strong>,</p>
        <p>Hệ thống đã nhận được thanh toán cho đơn hàng <strong>#{{OrderCode}}</strong> qua phương thức <strong>{{PaymentMethod}}</strong>.</p>
        
        <div style=""background-color: #f0fdf4; border-left: 4px solid #16a34a; padding: 15px; margin: 20px 0;"">
            <p style=""margin: 0; color: #15803d; font-weight: bold;"">Số tiền đã thanh toán: {{Amount}} VNĐ</p>
            <p style=""margin: 5px 0 0 0; font-size: 13px; color: #166534;"">Đơn hàng của bạn đang được chuẩn bị để đóng gói và vận chuyển.</p>
        </div>
    </div>
    <div style=""background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d;"">
        &copy; 2026 PickleHub Store. Mọi quyền được bảo lưu.
    </div>
</div>",
                Version = 1,
                UpdatedAt = now
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "OrderStatusUpdated",
                Subject = "[PickleHub] Cập nhật trạng thái đơn hàng #{{OrderCode}}",
                BodyHtml = @"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;"">
    <div style=""background-color: #2563eb; color: #ffffff; padding: 20px; text-align: center;"">
        <h1 style=""margin: 0; font-size: 24px;"">PickleHub Store</h1>
        <p style=""margin: 5px 0 0 0; font-size: 14px;"">Trạng thái đơn hàng đã thay đổi</p>
    </div>
    <div style=""padding: 20px;"">
        <p>Xin chào <strong>{{CustomerName}}</strong>,</p>
        <p>Đơn hàng <strong>#{{OrderCode}}</strong> của bạn vừa được chuyển sang trạng thái: <strong style=""color: #2563eb;"">{{OrderStatusName}}</strong>.</p>
        
        <p>Mã vận đơn: <strong>{{TrackingNumber}}</strong></p>
        <p>Bạn có thể theo dõi hành trình giao hàng bằng cách bấm vào nút bên dưới:</p>
        
        <div style=""text-align: center; margin: 25px 0;"">
            <a href=""{{TrackingUrl}}"" style=""background-color: #2563eb; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 6px; font-weight: bold; display: inline-block;"">Theo dõi vận đơn</a>
        </div>
    </div>
    <div style=""background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d;"">
        &copy; 2026 PickleHub Store. Mọi quyền được bảo lưu.
    </div>
</div>",
                Version = 1,
                UpdatedAt = now
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "OrderCancelled",
                Subject = "[PickleHub] Đơn hàng #{{OrderCode}} đã bị hủy",
                BodyHtml = @"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;"">
    <div style=""background-color: #dc2626; color: #ffffff; padding: 20px; text-align: center;"">
        <h1 style=""margin: 0; font-size: 24px;"">PickleHub Store</h1>
        <p style=""margin: 5px 0 0 0; font-size: 14px;"">Đơn hàng đã bị hủy</p>
    </div>
    <div style=""padding: 20px;"">
        <p>Xin chào <strong>{{CustomerName}}</strong>,</p>
        <p>Đơn hàng <strong>#{{OrderCode}}</strong> của bạn đã bị hủy bởi <strong>{{CancelledByLabel}}</strong>.</p>

        <div style=""background-color: #fef2f2; border-left: 4px solid #dc2626; padding: 15px; margin: 20px 0;"">
            <p style=""margin: 0; color: #991b1b; font-weight: bold;"">Lý do hủy:</p>
            <p style=""margin: 5px 0 0 0; color: #7f1d1d;"">{{CancelReason}}</p>
        </div>

        <p>Nếu bạn đã thanh toán cho đơn hàng này, số tiền sẽ được hoàn lại theo chính sách hoàn tiền của PickleHub. Nếu có thắc mắc, vui lòng liên hệ bộ phận hỗ trợ khách hàng.</p>
    </div>
    <div style=""background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d;"">
        &copy; 2026 PickleHub Store. Mọi quyền được bảo lưu.
    </div>
</div>",
                Version = 1,
                UpdatedAt = now
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "UserRegistered",
                Subject = "[PickleHub] Xác thực địa chỉ Email đăng ký tài khoản",
                BodyHtml = @"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;"">
    <div style=""background-color: #16a34a; color: #ffffff; padding: 20px; text-align: center;"">
        <h1 style=""margin: 0; font-size: 24px;"">PickleHub Store</h1>
        <p style=""margin: 5px 0 0 0; font-size: 14px;"">Chào mừng bạn gia nhập cộng đồng PickleHub</p>
    </div>
    <div style=""padding: 20px;"">
        <p>Xin chào <strong>{{CustomerName}}</strong>,</p>
        <p>Cảm ơn bạn đã đăng ký tài khoản tại PickleHub! Vui lòng xác thực địa chỉ email để kích hoạt tài khoản và bắt đầu mua sắm.</p>

        <div style=""text-align: center; margin: 25px 0;"">
            <a href=""{{VerificationUrl}}"" style=""background-color: #16a34a; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 6px; font-weight: bold; display: inline-block;"">Xác thực tài khoản ngay</a>
        </div>

        <p style=""font-size: 13px; color: #6c757d;"">Nếu nút bấm không hoạt động, bạn có thể copy đường dẫn sau vào trình duyệt: {{VerificationUrl}}</p>
        <p style=""font-size: 13px; color: #6c757d;"">Nếu bạn không thực hiện đăng ký này, vui lòng bỏ qua email.</p>
    </div>
    <div style=""background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d;"">
        &copy; 2026 PickleHub Store. Mọi quyền được bảo lưu.
    </div>
</div>",
                Version = 1,
                UpdatedAt = now
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "PasswordResetRequested",
                Subject = "[PickleHub] Yêu cầu đặt lại mật khẩu tài khoản",
                BodyHtml = @"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;"">
    <div style=""background-color: #d97706; color: #ffffff; padding: 20px; text-align: center;"">
        <h1 style=""margin: 0; font-size: 24px;"">PickleHub Store</h1>
        <p style=""margin: 5px 0 0 0; font-size: 14px;"">Yêu cầu đặt lại mật khẩu</p>
    </div>
    <div style=""padding: 20px;"">
        <p>Xin chào <strong>{{CustomerName}}</strong>,</p>
        <p>Chúng tôi vừa nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Nhấp vào nút bên dưới để tạo mật khẩu mới:</p>

        <div style=""text-align: center; margin: 25px 0;"">
            <a href=""{{ResetUrl}}"" style=""background-color: #d97706; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 6px; font-weight: bold; display: inline-block;"">Đặt lại mật khẩu</a>
        </div>

        <div style=""background-color: #fffbeb; border-left: 4px solid #d97706; padding: 15px; margin: 20px 0;"">
            <p style=""margin: 0; color: #92400e; font-size: 13px;"">Liên kết này có hiệu lực trong <strong>15 phút</strong>. Nếu bạn không gửi yêu cầu này, vui lòng bỏ qua email và mật khẩu của bạn sẽ không bị thay đổi.</p>
        </div>
    </div>
    <div style=""background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d;"">
        &copy; 2026 PickleHub Store. Mọi quyền được bảo lưu.
    </div>
</div>",
                Version = 1,
                UpdatedAt = now
            }

        );
    }
}
