using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Categories.CreateCategory
{
    public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục không được để trống.")
                .MaximumLength(200).WithMessage("Tên danh mục tối đa 200 ký tự.");
        }
    }
}
