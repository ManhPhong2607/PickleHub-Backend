using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Products.AddProductVarriant
{
    public class AddProductVariantValidator : AbstractValidator<AddProductVariantCommand>
    {
        public AddProductVariantValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Thiếu Id sản phẩm.");

            RuleFor(x => x.Sku)
                .NotEmpty().WithMessage("SKU không được để trống.")
                .MaximumLength(100).WithMessage("SKU tối đa 100 ký tự.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Giá biến thể phải lớn hơn 0.");
        }
    }
}
