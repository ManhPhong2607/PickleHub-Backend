using FluentValidation;

namespace PickleHub.Authen.Application.Features.Auth.Login
{
    public class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được rỗng")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được rỗng")
                .MinimumLength(6).WithMessage("Mật khẩu ít nhất 8 kí tự");
        }
    }
}
