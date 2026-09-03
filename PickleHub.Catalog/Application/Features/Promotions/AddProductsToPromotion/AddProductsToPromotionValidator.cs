using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Promotions.AddProductsToPromotion
{
    public class AddProductsToPromotionValidator : AbstractValidator<AddProductsToPromotionCommand>
    {
        public AddProductsToPromotionValidator() 
        {
            RuleFor(x => x.Items)
               .Must(items => items.Select(x => x.ProductId)
               .Distinct().Count() == items.Count)
               .WithMessage("Không được có sản phẩm trùng lặp trong danh sách.");
        }
    }
}
