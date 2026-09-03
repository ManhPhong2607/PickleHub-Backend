using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Payment.Application.Common.Interfaces;
using PickleHub.Payment.Application.Features.Refunds.GetRefundRequests;
using PickleHub.Payment.Domain.Interfaces;

namespace PickleHub.Payment.Application.Features.Refunds.GetRefundByOrder;

public record GetRefundByOrderQuery(Guid OrderId) : IRequest<RefundRequestDto?>;

public class GetRefundByOrderQueryHandler(
    IPaymentDbContext db,
    IOrderClient orderClient) 
    : IRequestHandler<GetRefundByOrderQuery, RefundRequestDto?>
{
    public async Task<RefundRequestDto?> Handle(GetRefundByOrderQuery request, CancellationToken ct)
    {
        // 1. Kiểm tra trạng thái đơn hàng từ CartOrder service
        OrderDetailsDto? order = null;
        try
        {
            order = await orderClient.GetOrderDetailsAsync(request.OrderId, ct);
        }
        catch
        {
            // fallback if service unavailable
        }

        // Nếu đơn hàng còn hoạt động (chưa bị hủy Cancelled) -> Tuyệt đối không trả về thông tin hoàn tiền
        if (order != null && !order.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            // Xoá bản ghi rác nếu có từ trước
            var obsoleteRefund = await db.RefundRequests.FirstOrDefaultAsync(x => x.OrderId == request.OrderId, ct);
            if (obsoleteRefund != null && obsoleteRefund.Status != Domain.Enums.RefundStatus.Completed)
            {
                db.RefundRequests.Remove(obsoleteRefund);
                await db.SaveChangesAsync(ct);
            }
            return null;
        }

        var r = await db.RefundRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, ct);

        // Chỉ tạo bù RefundRequest nếu đơn hàng THỰC SỰ ĐÃ HỦY (Cancelled) và đã THANH TOÁN (Paid)
        if (r == null)
        {
            if (order != null && order.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == request.OrderId, ct);
                if (payment != null && payment.Status == PickleHub.Common.Enums.PaymentStatus.Paid)
                {
                    var newRefund = new Domain.Entities.RefundRequest
                    {
                        Id = Guid.NewGuid(),
                        PaymentId = payment.Id,
                        OrderId = request.OrderId,
                        UserId = payment.UserId,
                        Amount = payment.Amount,
                        Reason = "Đơn hàng thanh toán PayOS bị hủy",
                        BankCode = null,
                        AccountNumber = null,
                        AccountName = null,
                        Status = Domain.Enums.RefundStatus.WaitingForBankInfo,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.RefundRequests.Add(newRefund);
                    await db.SaveChangesAsync(ct);
                    r = newRefund;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return new RefundRequestDto(
            r.Id,
            r.PaymentId,
            r.OrderId,
            r.UserId,
            r.Amount,
            r.Reason,
            r.BankCode,
            r.AccountNumber,
            r.AccountName,
            r.Status,
            r.AdminNote,
            r.BankTransactionReference,
            r.CreatedAt,
            r.ProcessedAt,
            r.ProcessedBy
        );
    }
}
