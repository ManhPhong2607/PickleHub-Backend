namespace PickleHub.Common.Events.Payment;

/// <summary>
/// Event phát ra khi cổng thanh toán PayOS ghi nhận giao dịch thất bại.
/// Khớp chính xác với mô tả trong EventContract.md.
/// </summary>
public record PaymentFailedEvent
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }
}
