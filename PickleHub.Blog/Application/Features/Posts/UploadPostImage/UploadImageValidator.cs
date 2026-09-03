using FluentValidation;

namespace PickleHub.Blog.Application.Features.Posts.UploadPostImage
{
    public class UploadPostCoverImageValidator : AbstractValidator<UploadPostCoverImageCommand>
    {
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public UploadPostCoverImageValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("File không được để trống.")
                .Must(f => f.Length > 0).WithMessage("File không được rỗng.")
                .Must(f => f.Length <= MaxFileSizeBytes).WithMessage("Ảnh bìa không được vượt quá 10MB.")
                .Must(f => AllowedExtensions.Contains(
                    Path.GetExtension(f.FileName).ToLowerInvariant()))
                .WithMessage("Ảnh bìa chỉ chấp nhận .jpg, .jpeg, .png, .webp.");
        }
    }

    public class UploadInlineMediaValidator : AbstractValidator<UploadInlineMediaCommand>
    {
        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private static readonly string[] AllowedVideoExtensions = [".mp4", ".webm", ".mov"];
        private const long MaxImageSizeBytes = 10 * 1024 * 1024;  // 10 MB
        private const long MaxVideoSizeBytes = 100 * 1024 * 1024; // 100 MB

        public UploadInlineMediaValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("File không được để trống.")
                .Must(f => f.Length > 0).WithMessage("File không được rỗng.")
                .Must(f => AllowedImageExtensions.Concat(AllowedVideoExtensions)
                    .Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
                .WithMessage("Chỉ chấp nhận ảnh (.jpg, .jpeg, .png, .webp) hoặc video (.mp4, .webm, .mov).")
                .Must(f =>
                {
                    var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                    return AllowedVideoExtensions.Contains(ext)
                        ? f.Length <= MaxVideoSizeBytes
                        : f.Length <= MaxImageSizeBytes;
                })
                .WithMessage("Ảnh tối đa 10MB, video tối đa 100MB.");
        }
    }
}
