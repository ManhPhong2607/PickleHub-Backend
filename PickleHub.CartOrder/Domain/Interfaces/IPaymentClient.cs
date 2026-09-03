using System;
using System.Threading;
using System.Threading.Tasks;

namespace PickleHub.CartOrder.Domain.Interfaces;

public interface IPaymentClient
{
    Task<PaymentLinkResponseDto?> CreatePaymentLinkAsync(Guid orderId, decimal amount, CancellationToken ct = default);
}

public class PaymentLinkResponseDto
{
    public Guid PaymentId { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
}
