using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using PickleHub.CartOrder.Domain.Interfaces;

using Microsoft.Extensions.Configuration;

namespace PickleHub.CartOrder.Infrastructure.HttpClients;

/// <summary>
/// Thực hiện cuộc gọi HTTP vật lý đến Customer Service sử dụng header bảo mật nội bộ X-Internal-Key.
/// </summary>
public class CustomerHttpClient(HttpClient httpClient, IConfiguration config) : ICustomerClient
{
    private HttpRequestMessage BuildInternalRequest(string path)
    {
        var internalToken = config["Security:InternalApiKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Internal-Key", internalToken);
        return request;
    }
    public async Task<CustomerDto?> GetCustomerDetailsAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.SendAsync(BuildInternalRequest($"internal/customers/{customerId}"), ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Không thể kết nối đến Customer Service để lấy thông tin khách hàng: {ex.Message}", ex);
        }
    }

    public async Task<CustomerAddressDto?> GetAddressByIdAsync(Guid addressId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.SendAsync(BuildInternalRequest($"internal/customers/addresses/{addressId}"), ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CustomerAddressDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Không thể kết nối đến Customer Service để lấy thông tin địa chỉ: {ex.Message}", ex);
        }
    }
}
