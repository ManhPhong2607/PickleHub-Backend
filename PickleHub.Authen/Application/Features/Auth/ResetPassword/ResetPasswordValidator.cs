using FluentValidation;

namespace PickleHub.Authen.Application.Features.Auth.ResetPassword
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.");
        }
    }
}
