using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickleHub.Common.Enums;
using PickleHub.Common.Events.Order;
using PickleHub.Payment.Application.Common.Interfaces;
using PickleHub.Payment.Domain.Entities;
using PickleHub.Payment.Domain.Enums;

namespace PickleHub.Payment.Infrastructure.Consumers;

public class OrderCancelledConsumer(
    IPaymentDbContext db,
    ILogger<OrderCancelledConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("[OrderCancelledConsumer] Nhận sự kiện hủy đơn #{OrderId} của khách hàng {CustomerId}", msg.OrderId, msg.CustomerId);

        // 1. Tìm thông tin thanh toán của đơn hàng này
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == msg.OrderId);
        if (payment == null)
        {
            logger.LogInformation("[OrderCancelledConsumer] Đơn hàng #{OrderId} không có giao dịch thanh toán trong hệ thống Payment.", msg.OrderId);
            return;
        }

        // 2. Chỉ tạo RefundRequest nếu đơn hàng đã thanh toán thành công (PaymentStatus == Paid)
        if (payment.Status != PaymentStatus.Paid)
        {
            logger.LogInformation("[OrderCancelledConsumer] Đơn hàng #{OrderId} có trạng thái thanh toán là {Status} (chưa thanh toán), không cần hoàn tiền.", msg.OrderId, payment.Status);
            return;
        }

        // 3. Đảm bảo Idempotency: Không tạo trùng RefundRequest cho cùng một OrderId
        var existingRefund = await db.RefundRequests.AnyAsync(r => r.OrderId == msg.OrderId);
        if (existingRefund)
        {
            logger.LogWarning("[OrderCancelledConsumer] Đơn hàng #{OrderId} đã tồn tại RefundRequest từ trước.", msg.OrderId);
            return;
        }

        // 4. Tạo yêu cầu hoàn tiền bán tự động (Refund Request)
        // Lưu ý: Thông tin STK nhận tiền phải để TRỐNG để khách hàng hoặc admin tự cung cấp chính xác, không tự ý gán thông tin ảo từ PayOS
        var refundRequest = new RefundRequest
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            OrderId = msg.OrderId,
            UserId = msg.CustomerId,
            Amount = payment.Amount,
            Reason = string.IsNullOrWhiteSpace(msg.CancelReason) ? "Khách hàng / Hệ thống hủy đơn hàng" : msg.CancelReason,
            BankCode = null,
            AccountNumber = null,
            AccountName = null,
            Status = RefundStatus.WaitingForBankInfo,
            CreatedAt = DateTime.UtcNow
        };

        db.RefundRequests.Add(refundRequest);
        await db.SaveChangesAsync();

        logger.LogInformation("[OrderCancelledConsumer] Đã tạo thành công RefundRequest #{RefundId} số tiền {Amount} VNĐ cho đơn hàng #{OrderId}",
            refundRequest.Id, refundRequest.Amount, msg.OrderId);
    }
}
