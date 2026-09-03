using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Payment.Application.Common.Interfaces;
using PickleHub.Payment.Domain.Entities;
using PickleHub.Payment.Domain.Enums;

namespace PickleHub.Payment.Application.Features.Refunds.UpdateRefundBankInfo;

public record UpdateRefundBankInfoCommand(
    Guid OrderId,
    string BankCode,
    string AccountNumber,
    string AccountName,
    Guid? UserId = null
) : IRequest<bool>;

public class UpdateRefundBankInfoCommandHandler(IPaymentDbContext db)
    : IRequestHandler<UpdateRefundBankInfoCommand, bool>
{
    public async Task<bool> Handle(UpdateRefundBankInfoCommand request, CancellationToken ct)
    {
        var refund = await db.RefundRequests
            .FirstOrDefaultAsync(r => r.OrderId == request.OrderId, ct);

        if (refund == null)
        {
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == request.OrderId, ct);
            if (payment == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy giao dịch thanh toán cho đơn hàng {request.OrderId}.");
            }

            refund = new RefundRequest
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                OrderId = request.OrderId,
                UserId = request.UserId ?? payment.UserId,
                Amount = payment.Amount,
                Reason = "Khách hàng cung cấp thông tin tài khoản nhận hoàn tiền",
                BankCode = request.BankCode.Trim().ToUpper(),
                AccountNumber = request.AccountNumber.Trim(),
                AccountName = request.AccountName.Trim().ToUpper(),
                Status = RefundStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            db.RefundRequests.Add(refund);
        }
        else
        {
            refund.BankCode = request.BankCode.Trim().ToUpper();
            refund.AccountNumber = request.AccountNumber.Trim();
            refund.AccountName = request.AccountName.Trim().ToUpper();

            if (refund.Status == RefundStatus.WaitingForBankInfo)
            {
                refund.Status = RefundStatus.Pending;
            }
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
