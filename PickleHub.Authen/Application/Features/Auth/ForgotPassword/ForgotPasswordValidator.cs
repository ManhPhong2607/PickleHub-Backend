using FluentValidation;

namespace PickleHub.Authen.Application.Features.Auth.ForgotPassword
{
    public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordValidator()
        {
            RuleFor(x => x.Email)
              .NotEmpty().WithMessage("Email không được rỗng")
              .EmailAddress().WithMessage("Email không hợp lệ");
        }
    }
}
