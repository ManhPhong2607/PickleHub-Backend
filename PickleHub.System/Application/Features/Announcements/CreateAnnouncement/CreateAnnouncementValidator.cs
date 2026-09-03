using FluentValidation;

namespace PickleHub.System.Application.Features.Announcements.CreateAnnouncement
{
    public class CreateAnnouncementValidator : AbstractValidator<CreateAnnouncementCommand>
    {
        public CreateAnnouncementValidator() 
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Vui lòng nhập tiêu đề.")
                .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Vui lòng nhập nội dung.")
                .MaximumLength(1000).WithMessage("Nội dung không được vượt quá 1000 ký tự.");

            RuleFor(x => x.EndsAt)
                .GreaterThan(x => x.StartsAt)
                .When(x => x.StartsAt.HasValue && x.EndsAt.HasValue)
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        }
    }
}
