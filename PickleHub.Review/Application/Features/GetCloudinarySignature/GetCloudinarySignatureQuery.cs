using MediatR;
using PickleHub.Review.Domain.Interfaces;

namespace PickleHub.Review.Application.Features.GetCloudinarySignature;

public record GetCloudinarySignatureQuery(
    Guid UserId,
    Guid OrderId,
    Guid ProductId
) : IRequest<CloudinarySignatureResponseDto>;

public record CloudinarySignatureResponseDto(
    string CloudName,
    string ApiKey,
    long Timestamp,
    string Signature,
    string Folder
);

public class GetCloudinarySignatureQueryHandler(
    IConfiguration config,
    IOrderClient orderClient
) : IRequestHandler<GetCloudinarySignatureQuery, CloudinarySignatureResponseDto>
{
    public async Task<CloudinarySignatureResponseDto> Handle(GetCloudinarySignatureQuery request, CancellationToken ct)
    {
        // 1. Kiểm tra xem User này có thật sự mua đơn hàng này và có sản phẩm này hay không (Signed Upload Verification)
        bool isVerifiedPurchase = await orderClient.VerifyOrderCompletedAsync(
            request.UserId, request.OrderId, request.ProductId, ct);

        if (!isVerifiedPurchase)
        {
            throw new InvalidOperationException("Bạn không có quyền tải ảnh cho đơn hàng chưa hoàn tất hoặc không sở hữu.");
        }

        var cloudName = config["Cloudinary:CloudName"] ?? "picklehub";
        var apiKey = config["Cloudinary:ApiKey"] ?? throw new InvalidOperationException("Thiếu cấu hình Cloudinary:ApiKey");
        var apiSecret = config["Cloudinary:ApiSecret"] ?? throw new InvalidOperationException("Thiếu cấu hình Cloudinary:ApiSecret");

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string folder = "picklehub/reviews";

        // 2. Tự tính SHA1 theo đúng Cloudinary Signed Upload spec:
        //    Sort params alphabetically → join "key=value&..." → append apiSecret → SHA1 hex
        var sortedParams = new SortedDictionary<string, string>
        {
            { "folder", folder },
            { "timestamp", timestamp.ToString() }
        };

        string paramString = string.Join("&", sortedParams.Select(p => $"{p.Key}={p.Value}"));
        string toSign = paramString + apiSecret;

        byte[] hashBytes = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(toSign));
        string signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return new CloudinarySignatureResponseDto(
            CloudName: cloudName,
            ApiKey: apiKey,
            Timestamp: timestamp,
            Signature: signature,
            Folder: folder
        );
    }
}
