using Microsoft.EntityFrameworkCore;
using PickleHub.System.Domain.Entities;
using PickleHub.System.Infrastructure.Persistence;

namespace PickleHub.System.Extensions
{
    public static class WebApplicationExtensions
    {
        public static async Task SeedDefaultConfigsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

            await db.Database.MigrateAsync();

            if (db.SystemConfigs.Any()) return;

            db.SystemConfigs.AddRange(
                SystemConfig.Create(
                    "low_stock_threshold", "5",
                    "Ngưỡng tồn kho để cảnh báo sắp hết hàng"),
                SystemConfig.Create(
                    "shipping_fee_default", "30000",
                    "Phí vận chuyển mặc định (VNĐ)"),
                SystemConfig.Create(
                    "order_cancel_deadline_hours", "1",
                    "Số giờ customer được tự hủy đơn sau khi đã xác nhận")
            );

            await db.SaveChangesAsync();
        }
    }
}
