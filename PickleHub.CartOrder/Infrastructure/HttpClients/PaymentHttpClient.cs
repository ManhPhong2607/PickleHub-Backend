using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PickleHub.CartOrder.Domain.Interfaces;

namespace PickleHub.CartOrder.Infrastructure.HttpClients;

public class PaymentHttpClient(HttpClient httpClient, IConfiguration config) : IPaymentClient
{
    public async Task<PaymentLinkResponseDto?> CreatePaymentLinkAsync(Guid orderId, decimal amount, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "internal/payments/create-link");
            
            var internalToken = config["Security:InternalApiKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
            request.Headers.Add("X-Internal-Key", internalToken);
            request.Content = JsonContent.Create(new { OrderId = orderId, Amount = amount });

            var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PaymentLinkResponseDto>(cancellationToken: ct);
            }
            
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"Cổng thanh toán phản hồi lỗi: {response.StatusCode} - {err}");
        }
        catch (Exception ex) when (ex is not Exception || !ex.Message.StartsWith("Cổng thanh toán"))
        {
            throw new Exception("Không thể kết nối đến Payment Service để tạo mã thanh toán.", ex);
        }
    }
}
