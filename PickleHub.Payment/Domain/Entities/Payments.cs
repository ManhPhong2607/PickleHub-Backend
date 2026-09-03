using PickleHub.Common.Enums;

namespace PickleHub.Payment.Domain.Entities;

public class Payments
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string? IdempotencyToken { get; set; }
    
    // PayOS yêu cầu mã đơn hàng (orderCode) phải là kiểu số nguyên (long/int64)
    // Nên sinh một mã số ngẫu nhiên hoặc tuần tự để giao tiếp với PayOS và lưu ở đây
    public long OrderCode { get; set; }
    
    // ID của liên kết thanh toán do PayOS trả về (kiểu string, ví dụ: "7dfc92...")
    public string PaymentLinkId { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
