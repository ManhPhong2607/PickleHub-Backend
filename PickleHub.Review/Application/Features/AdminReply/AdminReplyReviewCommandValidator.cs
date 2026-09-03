using FluentValidation;

namespace PickleHub.Review.Application.Features.AdminReply;

public class AdminReplyReviewCommandValidator : AbstractValidator<AdminReplyReviewCommand>
{
    public AdminReplyReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("ID bài đánh giá không được để rỗng.");
        
        RuleFor(x => x.ReplyContent)
            .NotEmpty()
            .WithMessage("Nội dung phản hồi không được để trống.")
            .MaximumLength(2000)
            .WithMessage("Nội dung phản hồi không được vượt quá 2000 ký tự.");
    }
}
