using FluentValidation;

namespace PickleHub.Inventory.Application.Features.Inventory.UpdateThreshold
{
    public class UpdateThresholdValidator : AbstractValidator<UpdateThresholdCommand>
    {
        public UpdateThresholdValidator()
        {
            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.Threshold)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Ngưỡng cảnh báo không được âm.");
        }
    }
}
