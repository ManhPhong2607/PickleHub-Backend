using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PickleHub.Payment.Domain.Interfaces;

namespace PickleHub.Payment.Infrastructure.HttpClients;

public class OrderHttpClient(HttpClient httpClient, IConfiguration config) : IOrderClient
{
    public async Task<OrderDetailsDto?> GetOrderDetailsAsync(Guid orderId, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"internal/orders/{orderId}");
            
            var internalToken = config["Security:InternalApiKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
            request.Headers.Add("X-Internal-Key", internalToken);

            var response = await httpClient.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<OrderDetailsDto>(cancellationToken: ct);
        }
        catch (HttpRequestException)
        {
            throw new Exception("Không thể kết nối sang CartOrder Service để lấy thông tin đơn hàng.");
        }
    }
}
