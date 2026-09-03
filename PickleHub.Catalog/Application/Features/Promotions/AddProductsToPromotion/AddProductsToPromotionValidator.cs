using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Promotions.AddProductsToPromotion
{
    public class AddProductsToPromotionValidator : AbstractValidator<AddProductsToPromotionCommand>
    {
        public AddProductsToPromotionValidator() 
        {
            RuleFor(x => x.Items)
               .Cascade(CascadeMode.Stop)
               .NotNull().WithMessage("Danh sách sản phẩm không được để trống.")
               .NotEmpty().WithMessage("Danh sách sản phẩm không được để trống.")
               .Must(items => items.Select(x => x.ProductId)
               .Distinct().Count() == items.Count)
               .WithMessage("Không được có sản phẩm trùng lặp trong danh sách.");
        }
    }
}
