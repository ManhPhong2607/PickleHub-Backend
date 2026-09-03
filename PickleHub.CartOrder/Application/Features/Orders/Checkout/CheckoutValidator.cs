using FluentValidation;

namespace PickleHub.CartOrder.Application.Features.Orders.Checkout;

public class CheckoutValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty().WithMessage("Địa chỉ giao hàng (AddressId) không được để trống.");

        RuleFor(x => x.PaymentMethod)
            .Must(method => method == "COD" || method == "PayOS")
            .WithMessage("Phương thức thanh toán phải là 'COD' hoặc 'PayOS'.");
    }
}
