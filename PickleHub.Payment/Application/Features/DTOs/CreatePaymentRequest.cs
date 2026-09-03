using MediatR;

namespace PickleHub.Payment.Application.Features.DTOs;

public record CreatePaymentRequest(Guid OrderId, decimal Amount);
