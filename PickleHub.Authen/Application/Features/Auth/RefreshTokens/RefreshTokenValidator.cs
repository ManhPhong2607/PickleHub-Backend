using FluentValidation;

namespace PickleHub.Authen.Application.Features.Auth.RefreshTokens
{
    public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token không được rỗng");
        }
    }
}
