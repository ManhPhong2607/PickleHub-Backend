using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Common.Enums;
using PickleHub.Common.Events.Payment;
using PickleHub.Payment.Application.Common.Interfaces;
using PickleHub.Payment.Domain.Entities;
using PickleHub.Payment.Domain.Enums;

namespace PickleHub.Payment.Application.Features.Refunds.ProcessRefund;

public record ProcessRefundCommand(
    Guid RefundId,
    string Action, // "Approve" | "Reject"
    string? BankTransactionReference,
    string? AdminNote,
    string? ProcessedBy
) : IRequest<bool>;

public class ProcessRefundCommandHandler(
    IPaymentDbContext db,
    IPublishEndpoint publishEndpoint) : IRequestHandler<ProcessRefundCommand, bool>
{
    public async Task<bool> Handle(ProcessRefundCommand request, CancellationToken ct)
    {
        var refund = await db.RefundRequests
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == request.RefundId, ct);

        if (refund == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy yêu cầu hoàn tiền với ID: {request.RefundId}");
        }

        if (refund.Status != RefundStatus.Pending)
        {
            throw new InvalidOperationException($"Yêu cầu hoàn tiền này đã được xử lý trước đó (Trạng thái: {refund.Status}).");
        }

        bool isApprove = string.Equals(request.Action, "Approve", StringComparison.OrdinalIgnoreCase);

        if (isApprove)
        {
            refund.Status = RefundStatus.Completed;
            refund.BankTransactionReference = request.BankTransactionReference;
            refund.AdminNote = request.AdminNote;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.ProcessedBy = request.ProcessedBy ?? "Admin";

            // Cập nhật trạng thái Payment thành Refunded
            if (refund.Payment != null)
            {
                refund.Payment.Status = PaymentStatus.Refunded;
            }

            await publishEndpoint.Publish(new RefundCompletedEvent
            {
                RefundRequestId = refund.Id,
                OrderId = refund.OrderId,
                UserId = refund.UserId,
                Amount = refund.Amount,
                BankTransactionReference = refund.BankTransactionReference,
                AdminNote = refund.AdminNote,
                ProcessedAt = DateTime.UtcNow
            }, ct);
        }
        else
        {
            refund.Status = RefundStatus.Rejected;
            refund.AdminNote = request.AdminNote;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.ProcessedBy = request.ProcessedBy ?? "Admin";
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
