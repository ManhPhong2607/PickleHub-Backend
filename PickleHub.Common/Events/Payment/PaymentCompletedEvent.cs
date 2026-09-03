namespace PickleHub.Common.Events.Payment;

/// <summary>
/// Event phát ra khi cổng thanh toán PayOS ghi nhận giao dịch thành công.
/// Khớp chính xác với mô tả trong EventContract.md.
/// </summary>
public record PaymentCompletedEvent
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string Method { get; init; } = string.Empty; // "PayOS" | "COD"
    public DateTime PaidAt { get; init; }
}
