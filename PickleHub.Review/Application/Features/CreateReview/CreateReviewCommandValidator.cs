using FluentValidation;

namespace PickleHub.Review.Application.Features.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("ID người dùng không được để rỗng.");
        
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ID sản phẩm không được để rỗng.");

        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Mọi bài đánh giá đều phải thuộc về một đơn hàng đã mua.");
        
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Số sao đánh giá phải từ 1 đến 5 sao.");
        
        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Comment))
            .WithMessage("Nhận xét tối đa 1000 ký tự.");
        
        RuleFor(x => x.ImageUrls)
            .Must(x => x == null || x.Count <= 5)
            .WithMessage("Mỗi bài đánh giá chỉ được tải lên tối đa 5 hình ảnh.");
        
        RuleForEach(x => x.ImageUrls)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
            .WithMessage("URL hình ảnh không hợp lệ.");

        RuleFor(x => x.ProductVariantId)
            .NotEqual(Guid.Empty)
            .When(x => x.ProductVariantId.HasValue);
    }
}
