using FluentValidation;

namespace PickleHub.CartOrder.Application.Features.Cart.UpdateItem;

public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x.CartItemId)
            .NotEmpty().WithMessage("Mã dòng giỏ hàng (CartItemId) không được để trống.");
    }
}
