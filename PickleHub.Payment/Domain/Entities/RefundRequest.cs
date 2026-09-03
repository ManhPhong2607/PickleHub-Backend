using PickleHub.Payment.Domain.Enums;

namespace PickleHub.Payment.Domain.Entities;

public class RefundRequest
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? AdminNote { get; set; }
    public string? BankTransactionReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }

    public Payments? Payment { get; set; }
}
