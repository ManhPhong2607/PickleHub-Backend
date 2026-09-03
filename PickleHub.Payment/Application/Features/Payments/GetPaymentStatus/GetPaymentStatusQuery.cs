using MediatR;
using Microsoft.EntityFrameworkCore;
using PickleHub.Payment.Application.Common.Interfaces;

namespace PickleHub.Payment.Application.Features.Payments.GetPaymentStatus;

public record GetPaymentStatusQuery(Guid OrderId) : IRequest<PaymentStatusDto>;

public record PaymentStatusDto(Guid OrderId, Guid UserId, string Status, decimal Amount, DateTime? PaidAt = null);

public class GetPaymentStatusQueryHandler(IPaymentDbContext db) : IRequestHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    public async Task<PaymentStatusDto> Handle(GetPaymentStatusQuery request, CancellationToken ct)
    {
        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == request.OrderId, ct);

        if (payment is null)
        {
            return new PaymentStatusDto(request.OrderId, Guid.Empty, "None", 0);
        }

        return new PaymentStatusDto(
            payment.OrderId,
            payment.UserId,
            payment.Status.ToString(),
            payment.Amount,
            payment.PaidAt
        );
    }
}
