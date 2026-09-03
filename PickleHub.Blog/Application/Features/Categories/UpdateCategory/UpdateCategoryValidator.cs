using FluentValidation;

namespace PickleHub.Blog.Application.Features.Categories.UpdateCategory
{
    public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryValidator()
        {
            RuleFor(x => x.Name)
               .NotEmpty().WithMessage("Tên category không được để trống.")
               .MaximumLength(100).WithMessage("Tên category tối đa 100 kí tự.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Mô tả tối đa 500 kí tự.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị phải lớn hơn hoặc bằng 0.");
        }
    }
}
