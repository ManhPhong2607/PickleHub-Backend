using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Products.UpdateProductVariant
{
    public class UpdateProductVariantValidator : AbstractValidator<UpdateProductVariantCommand>
    {
        public UpdateProductVariantValidator() 
        {
            RuleFor(x => x.Sku)
                .NotEmpty().WithMessage("SKU không được để trống.")
                .MaximumLength(100).WithMessage("SKU tối đa 100 ký tự.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Giá biến thể phải lớn hơn 0.");
        }
    }
}
