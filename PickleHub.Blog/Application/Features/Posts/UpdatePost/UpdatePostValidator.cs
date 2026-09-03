using FluentValidation;

namespace PickleHub.Blog.Application.Features.Posts.UpdatePost
{
    public class UpdatePostValidator : AbstractValidator<UpdatePostCommand>
    {
        public UpdatePostValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề không được để trống.")
                .MaximumLength(200).WithMessage("Tiêu đề tối đa 200 ký tự.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung không được để trống.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Vui lòng chọn category.");

            RuleFor(x => x.Summary)
                .MaximumLength(500).WithMessage("Tóm tắt tối đa 500 ký tự.");

            RuleFor(x => x.SeoTitle)
                .MaximumLength(70).WithMessage("SEO Title tối đa 70 ký tự.");

            RuleFor(x => x.SeoDescription)
                .MaximumLength(160).WithMessage("SEO Description tối đa 160 ký tự.");

            RuleFor(x => x.RelatedProductIds)
                .Must(ids => ids == null || ids.Count <= 6)
                .WithMessage("Chỉ được gắn tối đa 6 sản phẩm liên quan cho 1 bài viết.");
        }
    }
}
