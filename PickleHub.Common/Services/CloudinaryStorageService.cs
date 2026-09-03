using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using PickleHub.Common.Interfaces;

namespace PickleHub.Common.Service
{
    public class CloudinaryStorageService : IStorageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryStorageService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"]
                ?? throw new InvalidOperationException("Cloudinary:CloudName chưa cấu hình.");

            var apiKey = configuration["Cloudinary:ApiKey"]
                ?? throw new InvalidOperationException("Cloudinary:ApiKey chưa cấu hình.");

            var apiSecret = configuration["Cloudinary:ApiSecret"]
                ?? throw new InvalidOperationException("Cloudinary:ApiSecret chưa cấu hình.");

            _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret))
            {
                Api = { Secure = true }
            };
        }

        public async Task<FileUploadResult> UploadAsync(
            Stream fileStream,
            string fileName,
            string folder,
            string resourceType = "image",
            CancellationToken ct = default)
        {
            string publicId;
            string secureUrl;
            int? width;
            int? height;
            long? bytes;
            string? error;

            if (resourceType.Equals("video", StringComparison.OrdinalIgnoreCase))
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = $"picklehub/{folder}",
                    UseFilename = true,
                    UniqueFilename = true
                };

                var result = await _cloudinary.UploadAsync(uploadParams, ct);
                error = result.Error?.Message;
                publicId = result.PublicId;
                secureUrl = result.SecureUrl?.ToString() ?? string.Empty;
                width = result.Width;
                height = result.Height;
                bytes = result.Bytes;
            }
            else
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = $"picklehub/{folder}",
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto"), // tự convert sang WebP
                    UseFilename = true,
                    UniqueFilename = true
                };

                var result = await _cloudinary.UploadAsync(uploadParams, ct);
                error = result.Error?.Message;
                publicId = result.PublicId;
                secureUrl = result.SecureUrl?.ToString() ?? string.Empty;
                width = result.Width;
                height = result.Height;
                bytes = result.Bytes;
            }

            if (error != null)
                throw new InvalidOperationException($"Cloudinary lỗi: {error}");

            return new FileUploadResult(
                PublicId: publicId,
                SecureUrl: secureUrl,
                ResourceType: resourceType.ToLowerInvariant(),
                Width: width,
                Height: height,
                SizeBytes: bytes
            );
        }

        public async Task DeleteAsync(string publicId, string resourceType = "image")
        {
            var resType = resourceType.Equals("video", StringComparison.OrdinalIgnoreCase)
                ? ResourceType.Video
                : ResourceType.Image;

            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = resType
            };

            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Result == "ok" || result.Result == "not found")
            {
                return;
            }

            throw new InvalidOperationException(
                $"Cloudinary xoá thất bại: {result.Result}"
            );
        }
    }
}
