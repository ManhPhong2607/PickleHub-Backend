using MediatR;
using PickleHub.Common.Interfaces;
using PickleHub.System.Application.Features.DTOs;

namespace PickleHub.System.Application.Features.Announcements.UploadAnnouncementImage
{
    // Không gắn với 1 announcement cụ thể nào (khác Catalog nơi ảnh gắn thẳng vào productId) -
    // vì lúc upload ảnh, announcement có thể còn chưa được tạo (đang ở form tạo mới).
    // (lúc xóa announcement hoặc đổi sang ảnh khác), Url để hiển thị.
    public record UploadAnnouncementImageCommand(Stream FileStream, string FileName) : IRequest<UploadImageResultDto>;

    public class UploadAnnouncementImageHandler : IRequestHandler<UploadAnnouncementImageCommand, UploadImageResultDto>
    {
        private readonly IStorageService _storage;

        public UploadAnnouncementImageHandler(IStorageService storage)
        {
            _storage = storage;
        }

        public async Task<UploadImageResultDto> Handle(UploadAnnouncementImageCommand request, CancellationToken ct)
        {
            string extension = Path.GetExtension(request.FileName).ToLowerInvariant();
            string resourceType = extension switch
            {
                ".mp4" or ".webm" or ".mov" => "video",
                _ => "image"
            };

            var result = await _storage.UploadAsync(
                request.FileStream, request.FileName, folder: "announcements", resourceType: resourceType, ct: ct);

            return new UploadImageResultDto
            {
                Url = result.SecureUrl,
                PublicId = result.PublicId
            };
        }
    }
}
