using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickleHub.Common.Interfaces
{
    public record FileUploadResult(
        string PublicId,
        string SecureUrl,
        string ResourceType,
        int? Width,
        int? Height,
        long? SizeBytes);

    public interface IStorageService
    {
        Task<FileUploadResult> UploadAsync(
            Stream fileStream,
            string fileName,
            string folder,
            string resourceType = "image",
            CancellationToken ct = default);

        Task DeleteAsync(string publicId, string resourceType = "image");
    }
}
