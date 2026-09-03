using FluentValidation;

namespace PickleHub.Authen.Application.Features.Auth.ResendVerification
{
    public class ResendVerificationValidator : AbstractValidator<ResendVerificationCommand>
    {
        public ResendVerificationValidator()
        {
            RuleFor(x => x.Email)
               .NotEmpty()
               .WithMessage("Email không được để trống.")
               .EmailAddress()
               .WithMessage("Email không hợp lệ.");
        }
    }
}
