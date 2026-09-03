using FluentValidation;

namespace PickleHub.Catalog.Application.Features.Products.AddProductImage
{
    public class AddProductImageValidator : AbstractValidator<AddProductImageCommand>
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".mp4", ".webm", ".mov" };
        private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB
        public AddProductImageValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Thiếu Id sản phẩm.");

            RuleFor(x => x.Files)
                .NotEmpty().WithMessage("Phải chọn ít nhất 1 file.")
                .Must(files => files == null || files.Count <= 7).WithMessage("Chỉ được tải lên tối đa 7 file trong 1 lần.")
                .Must(files => files != null && files.All(f => f.Length > 0)).WithMessage("File không được rỗng.")
                .Must(files => files != null && files.All(f => f.Length <= MaxFileSizeBytes)).WithMessage("File không được vượt quá 100MB.")
                .Must(files => files != null && files.All(f => AllowedExtensions.Contains(
                    Path.GetExtension(f.FileName).ToLowerInvariant())))
                .WithMessage("Chỉ chấp nhận file .jpg, .jpeg, .png, .webp, .mp4, .webm, .mov.");

        }
    }
}
