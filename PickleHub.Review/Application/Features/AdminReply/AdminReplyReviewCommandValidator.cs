using FluentValidation;

namespace PickleHub.Review.Application.Features.AdminReply;

public class AdminReplyReviewCommandValidator : AbstractValidator<AdminReplyReviewCommand>
{
    public AdminReplyReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("ID bài dánh giá không du?c d? r?ng.");
        RuleFor(x => x.ReplyContent)
            .NotEmpty()
            .WithMessage("N?i dung ph?n h?i không du?c d? r?ng.")
            .MaximumLength(1000)
            .WithMessage("N?i dung ph?n h?i t?i da 1000 ký t?.");
    }
}
