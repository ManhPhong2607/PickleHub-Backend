using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using PickleHub.CartOrder.Domain.Interfaces;

namespace PickleHub.CartOrder.Infrastructure.HttpClients;

public class SystemHttpClient(HttpClient httpClient) : ISystemClient
{
    public async Task<decimal> GetDefaultShippingFeeAsync(CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "system/configs/shipping_fee_default");
            request.Headers.Add("X-Internal-Service", "true");

            var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ConfigDto>(cancellationToken: ct);
                if (result != null && decimal.TryParse(result.Value, out var fee))
                {
                    return fee;
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi và trả về phí ship mặc định là 30000 VNĐ
        }

        return 30000m;
    }

    private class ConfigDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
