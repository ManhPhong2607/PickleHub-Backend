using FluentValidation;

namespace PickleHub.CartOrder.Application.Features.Cart.AddItem;

public class AddCartItemValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemValidator()
    {
        RuleFor(x => x.ProductVariantId)
            .NotEmpty().WithMessage("Mã biến thể sản phẩm (ProductVariantId) không được để trống.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Số lượng sản phẩm phải lớn hơn hoặc bằng 1.");
    }
}
