using FluentValidation;

namespace PickleHub.Authen.Application.Features.Auth.Register
{
    public class RegisterValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress().WithMessage("Email không hợp lệ.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.");
        }
    }
}
