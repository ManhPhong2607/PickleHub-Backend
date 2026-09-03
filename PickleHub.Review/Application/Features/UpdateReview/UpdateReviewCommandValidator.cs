using FluentValidation;

namespace PickleHub.Review.Application.Features.UpdateReview;

// Validator kiểm tra tính hợp lệ của dữ liệu đầu vào khi người dùng cập nhật bài đánh giá
public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("ID bài đánh giá không được để rỗng.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("ID người dùng không được để rỗng.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Số sao đánh giá phải từ 1 đến 5 sao.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Comment))
            .WithMessage("Nội dung nhận xét không được vượt quá 1000 ký tự.");

        RuleFor(x => x.ImageUrls)
            .Must(x => x == null || x.Count <= 5)
            .WithMessage("Mỗi bài đánh giá chỉ được tải lên tối đa 5 hình ảnh.");

        RuleForEach(x => x.ImageUrls)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
            .WithMessage("Định dạng URL hình ảnh không hợp lệ.");
    }
}
