using FluentValidation;

namespace PickleHub.Payment.Application.Features.Payments.CreatePayment;

/// <summary>
/// Trình kiểm tra tính hợp lệ của dữ liệu đầu vào khi tạo thanh toán (Mẫu).
/// </summary>
public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Mã đơn hàng (OrderId) không được để trống.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Số tiền thanh toán (Amount) phải lớn hơn 0.");
    }
}
