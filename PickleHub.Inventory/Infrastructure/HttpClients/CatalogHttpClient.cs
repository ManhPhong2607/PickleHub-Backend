using PickleHub.Inventory.Application.Common.Interfaces;

namespace PickleHub.Inventory.Infrastructure.HttpClients
{
    public class CatalogHttpClient(HttpClient httpClient, IConfiguration config, ILogger<CatalogHttpClient> logger) : ICatalogClient
    {
        public async Task<List<CatalogVariantDto>> GetAllVariantsAsync(CancellationToken ct = default)
        {
            try
            {
                var internalToken = config["Security:InternalApiKey"]
                    ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
                var request = new HttpRequestMessage(HttpMethod.Get, "internal/products/variants");
                request.Headers.Add("X-Internal-Key", internalToken);

                var response = await httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Không lấy được danh sách variant từ Catalog Service, status: {StatusCode}",
                        response.StatusCode);
                    return new List<CatalogVariantDto>();
                }

                return await response.Content.ReadFromJsonAsync<List<CatalogVariantDto>>(cancellationToken: ct)
                    ?? new List<CatalogVariantDto>();
            }
            catch (Exception ex)
            {
                // Không throw ra ngoài - đây chỉ là dữ liệu "bổ sung" cho template Excel (sản phẩm chưa từng nhập kho). Nếu Catalog Service không gọi được, template vẫn
                // nên xuất ra bình thường với những gì Inventory tự có, không chặn cả tính năng.
                logger.LogError(ex, "Lỗi khi gọi Catalog Service để lấy danh sách variant.");
                return new List<CatalogVariantDto>();
            }
        }
    }
}
