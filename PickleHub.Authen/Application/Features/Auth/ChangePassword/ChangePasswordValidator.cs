using FluentValidation;

namespace PickleHub.Authen.Application.Features.Auth.ChangePassword
{
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.")
                .NotEqual(x => x.OldPassword).WithMessage("Mật khẩu mới không được trùng mật khẩu cũ.");
        }
    }
}
