using MediatR;

namespace PickleHub.Payment.Application.Features.DTOs;

public record CreatePaymentResponse(
    Guid PaymentId,
    string CheckoutUrl,
    string PaymentLinkId,
    string? QrCode = null
    );
