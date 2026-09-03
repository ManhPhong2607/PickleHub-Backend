using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Products.CreateProduct
{
    public class CreateProductValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductValidator() 
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
                .MaximumLength(300).WithMessage("Tên sản phẩm không được vượt quá 300 ký tự.");

            //RuleFor(x => x.Description)
            //    .NotEmpty().WithMessage("Mô tả sản phẩm không được để trống.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Danh mục sản phẩm không được để trống.");

            RuleFor(x => x.BrandId)
                .NotEmpty().WithMessage("Thương hiệu sản phẩm không được để trống.");

        }
    }
}
