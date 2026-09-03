using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PickleHub.Blog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Blog.Application.Features.Posts.UploadPostImage
{
    // Set cover image (ảnh bìa) cho 1 bài viết — chỉ chấp nhận ảnh, không nhận video
    public record UploadPostCoverImageCommand(Guid PostId, IFormFile File) : IRequest<string>;

    public class UploadPostCoverImageHandler : IRequestHandler<UploadPostCoverImageCommand, string>
    {
        private readonly IPostRepository _postRepository;
        private readonly IStorageService _storageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UploadPostCoverImageHandler> _logger;

        public UploadPostCoverImageHandler(
            IPostRepository postRepository,
            IStorageService storageService,
            IUnitOfWork unitOfWork,
            ILogger<UploadPostCoverImageHandler> logger)
        {
            _postRepository = postRepository;
            _storageService = storageService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string> Handle(UploadPostCoverImageCommand request, CancellationToken ct)
        {
            var post = await _postRepository.GetByIdAsync(request.PostId, ct)
                ?? throw new NotFoundException("Không tìm thấy bài viết.");

            // Ghi nhớ publicId cũ TRƯỚC — sẽ xóa sau khi lưu DB thành công
            var oldPublicId = post.CoverImagePublicId;

            await using var stream = request.File.OpenReadStream();
            var result = await _storageService.UploadAsync(
                stream, request.File.FileName, "posts", "image", ct);

            post.SetCoverImage(result.SecureUrl, result.PublicId);

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch
            {
                // DB lỗi → xóa ảnh mới vừa upload, ảnh cũ vẫn còn nguyên → không có Broken Image
                await _storageService.DeleteAsync(result.PublicId);
                throw;
            }

            // DB lưu thành công → xóa ảnh cũ
            // Nếu Cloudinary fail ở đây: website vẫn hiển thị đúng ảnh mới, chỉ để lại file rác
            if (!string.IsNullOrEmpty(oldPublicId))
            {
                try
                {
                    await _storageService.DeleteAsync(oldPublicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Không thể xóa ảnh bìa cũ {PublicId} khỏi Cloudinary (postId={PostId}). File có thể bị bỏ lại, cần dọn thủ công.",
                        oldPublicId, request.PostId);
                }
            }

            return result.SecureUrl;
        }
    }

    // Upload ảnh/video để nhúng vào nội dung bài viết (rich text editor) — không gắn với Post nào cụ thể
    public record UploadInlineMediaCommand(IFormFile File) : IRequest<UploadInlineMediaResult>;

    public record UploadInlineMediaResult(string Url, string ResourceType);

    public class UploadInlineMediaHandler : IRequestHandler<UploadInlineMediaCommand, UploadInlineMediaResult>
    {
        private readonly IStorageService _storageService;

        public UploadInlineMediaHandler(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<UploadInlineMediaResult> Handle(UploadInlineMediaCommand request, CancellationToken ct)
        {
            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            var resourceType = extension switch
            {
                ".mp4" or ".webm" or ".mov" => "video",
                _ => "image"
            };

            await using var stream = request.File.OpenReadStream();
            var result = await _storageService.UploadAsync(
                stream, request.File.FileName, "posts/inline", resourceType, ct);

            return new UploadInlineMediaResult(result.SecureUrl, result.ResourceType);
        }
    }
}
