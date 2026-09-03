using MediatR;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PickleHub.Payment.Application.Features.DTOs;
using PickleHub.Common.Enums;
using PickleHub.Payment.Application.Common.Interfaces;
using PickleHub.Payment.Domain.Interfaces;

namespace PickleHub.Payment.Application.Features.Payments.CreatePayment;

// Command yêu cầu tạo link thanh toán mới.
public record CreatePaymentCommand(
    Guid OrderId,
    decimal Amount
) : IRequest<CreatePaymentResponse>;

public class CreatePaymentCommandHandler(
    IPaymentDbContext db,
    PayOSClient payOsClient,
    IConfiguration config,
    IOrderClient orderClient
) : IRequestHandler<CreatePaymentCommand, CreatePaymentResponse>
{
    public async Task<CreatePaymentResponse> Handle(CreatePaymentCommand request, CancellationToken ct)
    {
        // 0. Xác thực số tiền và đơn hàng từ CartOrder Service (Bảo mật giao dịch)
        var order = await orderClient.GetOrderDetailsAsync(request.OrderId, ct);
        if (order is null)
        {
            throw new KeyNotFoundException($"Không tìm thấy thông tin đơn hàng {request.OrderId} để thanh toán.");
        }

        if (order.TotalAmount != request.Amount)
        {
            throw new InvalidOperationException($"Số tiền thanh toán ({request.Amount} VNĐ) không khớp với giá trị thực tế của đơn hàng ({order.TotalAmount} VNĐ).");
        }

        // 1. Kiểm tra Idempotency: Tránh tạo trùng lặp giao dịch
        var existingPayment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == request.OrderId, ct);
        if (existingPayment is not null)
        {
            // Chặn tuyệt đối không cho tạo link lại nếu đơn hàng đã được thanh toán xong
            if (existingPayment.Status == PaymentStatus.Paid)
            {
                throw new InvalidOperationException("Đơn hàng này đã được thanh toán thành công trước đó.");
            }

            return new CreatePaymentResponse(
                existingPayment.Id,
                $"https://pay.payos.vn/web/{existingPayment.OrderCode}",
                existingPayment.PaymentLinkId
            );
        }

        // 2. Sinh mã đơn hàng ngẫu nhiên và kiểm tra tránh trùng lặp trong DB
        long orderCode;
        bool isDuplicate;
        do
        {
            orderCode = Random.Shared.Next(1000000, 99999999);
            isDuplicate = await db.Payments.AnyAsync(p => p.OrderCode == orderCode, ct);
        } while (isDuplicate);

        // 3. Lấy URL điều hướng từ configuration
        var returnUrl = config["PayOS:ReturnUrl"] ?? "http://localhost:3000/payment/success";
        var cancelUrl = config["PayOS:CancelUrl"] ?? "http://localhost:3000/payment/cancel";

        // 4. Khởi tạo dữ liệu thanh toán gửi lên PayOS
        var paymentData = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = (long)request.Amount,
            Description = $"Thanh toán đơn hàng #{orderCode}",
            CancelUrl = cancelUrl,
            ReturnUrl = returnUrl,
            Items = new List<PaymentLinkItem>()
        };

        // 5. Gọi API của SDK PayOS v2 để tạo link thanh toán thực tế
        PayOS.Models.V2.PaymentRequests.CreatePaymentLinkResponse createPaymentResult;
        try
        {
            createPaymentResult = await payOsClient.PaymentRequests.CreateAsync(paymentData);
        }
        catch (Exception ex)
        {
            throw new Exception($"Không thể khởi tạo cổng thanh toán PayOS: {ex.Message}", ex);
        }

        try
        {
            // 6. Lưu lịch sử giao dịch vào DB nội bộ (Chờ thanh toán)
            var paymentRecord = new PickleHub.Payment.Domain.Entities.Payments
            {
                Id = Guid.NewGuid(),
                OrderId = request.OrderId,
                UserId = order.UserId,
                OrderCode = orderCode,
                PaymentLinkId = createPaymentResult.PaymentLinkId,
                Amount = request.Amount,
                Method = "PayOS",
                Status = PaymentStatus.Unpaid,
                CreatedAt = DateTime.UtcNow
            };

            db.Payments.Add(paymentRecord);
            await db.SaveChangesAsync(ct);

            // 7. Trả về kết quả Checkout URL thực từ PayOS
            return new CreatePaymentResponse(
                paymentRecord.Id,
                createPaymentResult.CheckoutUrl,
                createPaymentResult.PaymentLinkId
            );
        }
        catch (Exception ex)
        {
            // Compensating Action: Hủy link trên PayOS nếu không lưu được vào DB
            try
            {
                await payOsClient.PaymentRequests.CancelAsync(orderCode, "Lỗi hệ thống lưu trữ giao dịch cục bộ.");
            }
            catch
            {
                // Bỏ qua lỗi hủy để tránh nuốt exception chính
            }

            throw new Exception($"Lỗi khi lưu trữ lịch sử giao dịch thanh toán: {ex.Message}", ex);
        }
    }
}
