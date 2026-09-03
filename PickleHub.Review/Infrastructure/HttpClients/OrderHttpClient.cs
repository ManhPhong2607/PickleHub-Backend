using System.Net;
using System.Net.Http.Json;
using PickleHub.Review.Domain.Interfaces;

namespace PickleHub.Review.Infrastructure.HttpClients;

public class OrderHttpClient(HttpClient httpClient) : IOrderClient
{
    public async Task<bool> VerifyOrderCompletedAsync(Guid userId, Guid orderId, Guid productId, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"orders/{orderId}/verify?userId={userId}&productId={productId}");
            request.Headers.Add("X-Internal-Service", "true");

            response = await httpClient.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Không thể kết nối tới dịch vụ Đơn hàng để xác thực giao dịch mua. Vui lòng thử lại sau.", ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Lỗi từ dịch vụ Đơn hàng (HTTP {(int)response.StatusCode}). Không thể xác thực đơn hàng.");
        }

        var result = await response.Content.ReadFromJsonAsync<VerifyOrderResponse>(cancellationToken: ct);
        return result?.IsCompleted ?? false;
    }

    private record VerifyOrderResponse(bool IsCompleted);
}
