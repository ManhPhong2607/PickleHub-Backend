using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PickleHub.Notification.Migrations
{
    /// <inheritdoc />
    public partial class InitialNotificationDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedEvents", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WebEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    OrderNotiEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PromotionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentNotiEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SystemNotiEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationSettings", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WebNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "VIEW_ORDER"),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebNotifications", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "NotificationTemplates",
                columns: new[] { "Id", "BodyHtml", "Name", "Subject", "UpdatedAt", "Version" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "\n<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;\">\n    <div style=\"background-color: #1a56db; color: #ffffff; padding: 20px; text-align: center;\">\n        <h1 style=\"margin: 0; font-size: 24px;\">PickleHub Store</h1>\n        <p style=\"margin: 5px 0 0 0; font-size: 14px;\">Xác nh?n don hàng thành công</p>\n    </div>\n    <div style=\"padding: 20px;\">\n        <p>Xin chào <strong>{{CustomerName}}</strong>,</p>\n        <p>C?m on b?n dã d?t hàng t?i PickleHub! Ðon hàng <strong>#{{OrderCode}}</strong> c?a b?n dã du?c ghi nh?n và dang ch? x? lý.</p>\n        \n        <table style=\"width: 100%; border-collapse: collapse; margin: 20px 0;\">\n            <tr style=\"background-color: #f8f9fa;\">\n                <th style=\"padding: 10px; border: 1px solid #dee2e6; text-align: left;\">Mã don hàng</th>\n                <td style=\"padding: 10px; border: 1px solid #dee2e6;\">#{{OrderCode}}</td>\n            </tr>\n            <tr>\n                <th style=\"padding: 10px; border: 1px solid #dee2e6; text-align: left;\">T?ng giá tr?</th>\n                <td style=\"padding: 10px; border: 1px solid #dee2e6; color: #1a56db; font-weight: bold;\">{{TotalAmount}} VNÐ</td>\n            </tr>\n            <tr style=\"background-color: #f8f9fa;\">\n                <th style=\"padding: 10px; border: 1px solid #dee2e6; text-align: left;\">Ð?a ch? giao hàng</th>\n                <td style=\"padding: 10px; border: 1px solid #dee2e6;\">{{ShippingAddress}}</td>\n            </tr>\n        </table>\n        \n        <p>Chúng tôi s? thông báo cho b?n ngay khi don hàng b?t d?u du?c v?n chuy?n.</p>\n    </div>\n    <div style=\"background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d;\">\n        &copy; 2026 PickleHub Store. M?i quy?n du?c b?o luu.\n    </div>\n</div>", "OrderConfirmation", "[PickleHub] Xác nh?n don hàng #{{OrderCode}}", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "\n<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;\">\n    <div style=\"background-color: #16a34a; color: #ffffff; padding: 20px; text-align: center;\">\n        <h1 style=\"margin: 0; font-size: 24px;\">PickleHub Store</h1>\n        <p style=\"margin: 5px 0 0 0; font-size: 14px;\">Xác nh?n thanh toán thành công</p>\n    </div>\n    <div style=\"padding: 20px;\">\n        <p>Xin chào <strong>{{CustomerName}}</strong>,</p>\n        <p>H? th?ng dã nh?n du?c thanh toán cho don hàng <strong>#{{OrderCode}}</strong> qua phuong th?c <strong>{{PaymentMethod}}</strong>.</p>\n        \n        <div style=\"background-color: #f0fdf4; border-left: 4px solid #16a34a; padding: 15px; margin: 20px 0;\">\n            <p style=\"margin: 0; color: #15803d; font-weight: bold;\">S? ti?n dã thanh toán: {{Amount}} VNÐ</p>\n            <p style=\"margin: 5px 0 0 0; font-size: 13px; color: #166534;\">Ðon hàng c?a b?n dang du?c chu?n b? d? dóng gói và v?n chuy?n.</p>\n        </div>\n    </div>\n    <div style=\"background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d;\">\n        &copy; 2026 PickleHub Store. M?i quy?n du?c b?o luu.\n    </div>\n</div>", "PaymentSuccess", "[PickleHub] Xác nh?n thanh toán don hàng #{{OrderCode}} thành công", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "\n<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;\">\n    <div style=\"background-color: #2563eb; color: #ffffff; padding: 20px; text-align: center;\">\n        <h1 style=\"margin: 0; font-size: 24px;\">PickleHub Store</h1>\n        <p style=\"margin: 5px 0 0 0; font-size: 14px;\">Tr?ng thái don hàng dã thay d?i</p>\n    </div>\n    <div style=\"padding: 20px;\">\n        <p>Xin chào <strong>{{CustomerName}}</strong>,</p>\n        <p>Ðon hàng <strong>#{{OrderCode}}</strong> c?a b?n v?a du?c chuy?n sang tr?ng thái: <strong style=\"color: #2563eb;\">{{OrderStatusName}}</strong>.</p>\n        \n        <p>Mã v?n don: <strong>{{TrackingNumber}}</strong></p>\n        <p>B?n có th? theo dõi hành trình giao hàng b?ng cách b?m vào nút bên du?i:</p>\n        \n        <div style=\"text-align: center; margin: 25px 0;\">\n            <a href=\"{{TrackingUrl}}\" style=\"background-color: #2563eb; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 6px; font-weight: bold; display: inline-block;\">Theo dõi v?n don</a>\n        </div>\n    </div>\n    <div style=\"background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d;\">\n        &copy; 2026 PickleHub Store. M?i quy?n du?c b?o luu.\n    </div>\n</div>", "OrderStatusUpdated", "[PickleHub] C?p nh?t tr?ng thái don hàng #{{OrderCode}}", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 }
                });

            migrationBuilder.CreateIndex(
                name: "idx_email_logs_to_status",
                table: "EmailLogs",
                columns: new[] { "ToEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Name",
                table: "NotificationTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_web_notifications_user_read",
                table: "WebNotifications",
                columns: new[] { "UserId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "ProcessedEvents");

            migrationBuilder.DropTable(
                name: "UserNotificationSettings");

            migrationBuilder.DropTable(
                name: "WebNotifications");
        }
    }
}
