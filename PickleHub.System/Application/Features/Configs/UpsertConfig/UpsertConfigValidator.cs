using FluentValidation;

namespace PickleHub.System.Application.Features.Configs.UpsertConfig
{
    public class UpsertConfigValidator : AbstractValidator<UpsertConfigCommand>
    {
        public UpsertConfigValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .MaximumLength(100)
                .Matches(@"^[a-z0-9_]+$")
                .WithMessage("Key chỉ được chứa chữ thường, số và dấu gạch dưới.");

            RuleFor(x => x.Value)
                .NotEmpty()
                .MaximumLength(500);
        }
    }
}
