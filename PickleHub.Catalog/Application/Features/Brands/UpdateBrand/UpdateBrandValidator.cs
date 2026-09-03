using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Brands.UpdateBrand
{
    public class UpdateBrandValidator : AbstractValidator<UpdateBrandCommand>
    {
        public UpdateBrandValidator() 
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên thương hiệu không được để trống")
                .MaximumLength(200).WithMessage("Tên thương hiệu tối đa 200 kí tự");
        }   
    }
}
