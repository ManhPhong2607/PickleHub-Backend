using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using PickleHub.Common.Interfaces;

namespace PickleHub.Common.Service
{
    public class CloudinaryStorageService : IStorageService
    {
        private readonly Cloudinary? _cloudinary;

        public CloudinaryStorageService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (!string.IsNullOrWhiteSpace(cloudName) &&
                !string.IsNullOrWhiteSpace(apiKey) &&
                !string.IsNullOrWhiteSpace(apiSecret))
            {
                _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret))
                {
                    Api = { Secure = true }
                };
            }
        }

        public async Task<FileUploadResult> UploadAsync(
            Stream fileStream,
            string fileName,
            string folder,
            string resourceType = "image",
            CancellationToken ct = default)
        {
            if (_cloudinary != null)
            {
                try
                {
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
                        if (result.Error == null && !string.IsNullOrEmpty(result.SecureUrl?.ToString()))
                        {
                            return new FileUploadResult(
                                PublicId: result.PublicId,
                                SecureUrl: result.SecureUrl.ToString(),
                                ResourceType: resourceType.ToLowerInvariant(),
                                Width: result.Width,
                                Height: result.Height,
                                SizeBytes: result.Bytes
                            );
                        }
                    }
                    else
                    {
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(fileName, fileStream),
                            Folder = $"picklehub/{folder}",
                            Transformation = new Transformation()
                                .Quality("auto")
                                .FetchFormat("auto"),
                            UseFilename = true,
                            UniqueFilename = true
                        };

                        var result = await _cloudinary.UploadAsync(uploadParams, ct);
                        if (result.Error == null && !string.IsNullOrEmpty(result.SecureUrl?.ToString()))
                        {
                            return new FileUploadResult(
                                PublicId: result.PublicId,
                                SecureUrl: result.SecureUrl.ToString(),
                                ResourceType: resourceType.ToLowerInvariant(),
                                Width: result.Width,
                                Height: result.Height,
                                SizeBytes: result.Bytes
                            );
                        }
                    }
                }
                catch
                {
                    // Fallback below
                }
            }

            // Local fallback storage
            var uniqueId = Guid.NewGuid().ToString("N");
            var ext = Path.GetExtension(fileName);
            using var memoryStream = new MemoryStream();
            if (fileStream.CanSeek) fileStream.Position = 0;
            await fileStream.CopyToAsync(memoryStream, ct);
            var bytes = memoryStream.ToArray();

            var mime = ext.ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                _ => "image/png"
            };
            var dataUrl = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";

            return new FileUploadResult(
                PublicId: uniqueId,
                SecureUrl: dataUrl,
                ResourceType: resourceType.ToLowerInvariant(),
                Width: 800,
                Height: 800,
                SizeBytes: bytes.Length
            );
        }

        public async Task DeleteAsync(string publicId, string resourceType = "image")
        {
            if (_cloudinary != null)
            {
                try
                {
                    var resType = resourceType.Equals("video", StringComparison.OrdinalIgnoreCase)
                        ? ResourceType.Video
                        : ResourceType.Image;

                    var deleteParams = new DeletionParams(publicId) { ResourceType = resType };
                    await _cloudinary.DestroyAsync(deleteParams);
                }
                catch
                {
                    // Ignore deletion error
                }
            }
        }
    }
}
