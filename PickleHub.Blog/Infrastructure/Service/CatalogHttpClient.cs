using PickleHub.Blog.Application.Common.Interfaces;

namespace PickleHub.Blog.Infrastructure.Service
{
    public class CatalogHttpClient : ICatalogClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CatalogHttpClient> _logger;

        public CatalogHttpClient(HttpClient httpClient, ILogger<CatalogHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<ProductSummary>> GetProductsByIdsAsync(List<Guid> productIds, CancellationToken ct = default)
        {
            if (productIds == null || productIds.Count == 0)
                return [];

            try
            {
                var response = await _httpClient.PostAsJsonAsync("internal/products/by-ids", productIds, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Catalog Service trả về {StatusCode} khi lấy related products.",
                        response.StatusCode);
                    return [];
                }

                var result = await response.Content.ReadFromJsonAsync<List<ProductSummary>>(cancellationToken: ct);
                return result ?? [];
            }
            catch (Exception ex)
            {
                // Không để lỗi từ Catalog làm sập trang chi tiết bài viết —
                // related products chỉ là phần bổ trợ, không phải nội dung chính.
                _logger.LogWarning(ex, "Không thể kết nối Catalog Service để lấy related products.");
                return [];
            }
        }
    }
}
