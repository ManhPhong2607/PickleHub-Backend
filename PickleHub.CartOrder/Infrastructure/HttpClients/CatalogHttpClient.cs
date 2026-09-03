using System.Net;
using System.Net.Http.Json;
using PickleHub.CartOrder.Domain.Interfaces;

namespace PickleHub.CartOrder.Infrastructure.HttpClients;

// Thực hiện cuộc gọi HTTP vật lý đến Catalog Service
// Gọi route "internal/products/{id}" (kèm X-Internal-Key) thay vì route public "products/{id}" -
// vì đây là cuộc gọi service-to-service để kiểm tra/lấy dữ liệu, không phải khách xem hàng,
// nên không được phép làm tăng ViewCount của sản phẩm (route public có side-effect này).
public class CatalogHttpClient(HttpClient httpClient, IConfiguration config) : ICatalogClient
{
    private HttpRequestMessage BuildInternalRequest(Guid productId)
    {
        var internalToken = config["Security:InternalApiKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
        var request = new HttpRequestMessage(HttpMethod.Get, $"internal/products/{productId}");
        request.Headers.Add("X-Internal-Key", internalToken);
        return request;
    }
    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.SendAsync(BuildInternalRequest(productId), ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Không thể kết nối đến Catalog Service để kiểm tra sản phẩm: {ex.Message}", ex);
        }
    }

    public async Task<CatalogProductDto?> GetProductDetailsAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.SendAsync(BuildInternalRequest(productId), ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CatalogProductDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Không thể kết nối đến Catalog Service để lấy thông tin sản phẩm: {ex.Message}", ex);
        }
    }
}
