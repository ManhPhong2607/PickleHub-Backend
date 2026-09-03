using FluentValidation;

namespace PickleHub.Customers.Application.Features.Customers.UpdateMe
{
    public class UpdateMeValidator : AbstractValidator<UpdateMeCommand>
    {
        public UpdateMeValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Vui lòng nhập họ tên.")
                .MaximumLength(200);

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$")
                .WithMessage("Số điện thoại không hợp lệ.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }
    }
}
