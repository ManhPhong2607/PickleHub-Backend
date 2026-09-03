using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Products.ReplaceProductImage
{
    public class ReplaceProductImageValidator : AbstractValidator<ReplaceProductImageCommand>
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".mp4", ".webm", ".mov" };
        private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB

        public ReplaceProductImageValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Thiếu Id sản phẩm.");

            RuleFor(x => x.ImageId)
                .NotEmpty().WithMessage("Thiếu Id ảnh cần thay.");

            RuleFor(x => x.File)
                .NotNull().WithMessage("File không được để trống.")
                .Must(f => f.Length > 0).WithMessage("File không được rỗng.")
                .Must(f => f.Length <= MaxFileSizeBytes).WithMessage("File không được vượt quá 100MB.")
                .Must(f => AllowedExtensions.Contains(
                    Path.GetExtension(f.FileName).ToLowerInvariant()))
                .WithMessage("Chỉ chấp nhận file .jpg, .jpeg, .png, .webp, .mp4, .webm, .mov.");
        }
    }
}
