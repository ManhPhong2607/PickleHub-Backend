namespace PickleHub.Common.Events.Payment;

public record RefundCompletedEvent
{
    public Guid RefundRequestId { get; init; }
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string? BankTransactionReference { get; init; }
    public string? AdminNote { get; init; }
    public DateTime ProcessedAt { get; init; }
}
