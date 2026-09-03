using FluentValidation;

namespace PickleHub.Inventory.Application.Features.Inventory.ImportStock
{
    public class ImportStockValidator : AbstractValidator<ImportStockCommand>
    {
        public ImportStockValidator()
        {
            RuleFor(x => x.ProductVariantId)
           .NotEmpty().WithMessage("ProductVariantId không được để trống.");

            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("ProductId không được để trống.");

            RuleFor(x => x.SkuSnapshot)
                .NotEmpty().WithMessage("SKU không được để trống.")
                .MaximumLength(100);
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng nhập kho phải lớn hơn 0.");
        }
    }
}
