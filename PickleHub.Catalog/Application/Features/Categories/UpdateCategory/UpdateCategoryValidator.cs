using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Categories.UpdateCategory
{
    public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryValidator()  
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục không được để trống.")
                .MaximumLength(200).WithMessage("Tên danh mục tối đa 200 ký tự.");

            RuleFor(x => x)
                .Must(x => x.ParentId != x.Id)
                .WithMessage("Danh mục không thể là cha của chính nó.")
                .OverridePropertyName("ParentId");
        }
    }
}
