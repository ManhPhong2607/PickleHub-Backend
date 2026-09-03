using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PickleHub.Common.Enums;
using PickleHub.Common.Events.Payment;
using PickleHub.Payment.Application.Common.Interfaces;

namespace PickleHub.Payment.Application.Features.Payments.GetPaymentStatus;

public record GetPaymentStatusQuery(Guid OrderId) : IRequest<PaymentStatusDto>;

public record PaymentStatusDto(Guid OrderId, Guid UserId, string Status, decimal Amount, DateTime? PaidAt = null, bool IsPaid = false);

public class GetPaymentStatusQueryHandler(
    IPaymentDbContext db,
    PayOSClient payOsClient,
    IPublishEndpoint publishEndpoint) : IRequestHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    public async Task<PaymentStatusDto> Handle(GetPaymentStatusQuery request, CancellationToken ct)
    {
        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == request.OrderId, ct);

        if (payment is null)
        {
            return new PaymentStatusDto(request.OrderId, Guid.Empty, "None", 0);
        }

        // Nếu trạng thái chưa thanh toán, chủ động hỏi PayOS Server (fallback phòng trường hợp Webhook localhost không tới được)
        if (payment.Status == PaymentStatus.Unpaid && payment.OrderCode > 0)
        {
            try
            {
                var payOsInfo = await payOsClient.PaymentRequests.GetAsync(payment.OrderCode);
                var statusStr = payOsInfo?.Status.ToString().ToUpper();
                if (statusStr == "PAID" || statusStr == "COMPLETED")
                {
                    payment.Status = PaymentStatus.Paid;
                    payment.PaidAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);

                    await publishEndpoint.Publish(new PaymentCompletedEvent
                    {
                        PaymentId = payment.Id,
                        OrderId = payment.OrderId,
                        UserId = payment.UserId,
                        Amount = payment.Amount,
                        Method = "PayOS",
                        PaidAt = DateTime.UtcNow
                    }, ct);
                }
            }
            catch
            {
                // Silently fallback to local database status
            }
        }

        return new PaymentStatusDto(
            payment.OrderId,
            payment.UserId,
            payment.Status.ToString(),
            payment.Amount,
            payment.PaidAt,
            payment.Status == PaymentStatus.Paid
        );
    }
}
