using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using PickleHub.CartOrder.Domain.Interfaces;

namespace PickleHub.CartOrder.Infrastructure.HttpClients;

// Thực hiện cuộc gọi HTTP vật lý đến Inventory Service để quản lý tồn kho đồng bộ
public class InventoryHttpClient(HttpClient httpClient, IConfiguration config) : IInventoryClient
{
    private string GetInternalToken() =>
        config["Security:InternalApiKey"]
        ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey cho CartOrder.");

    public async Task<bool> CheckStockAsync(Guid variantId, int quantity, CancellationToken ct = default)
    {
        try
        {
            // Route public (AllowAnonymous bên Inventory), không cần X-Internal-Key.
            var response = await httpClient.GetAsync(
                $"inventory/variants/{variantId}/check?requiredQuantity={quantity}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();

            var stock = await response.Content.ReadFromJsonAsync<CheckStockResponse>(cancellationToken: ct);

            // Dùng thẳng IsAvailable do Inventory tự tính (đã trừ ReservedQuantity) - không tự so sánh số lượng ở đây để tránh trùng logic với phía Inventory.
            return stock is not null && stock.IsAvailable;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Không thể kết nối đến Inventory Service để kiểm tra tồn kho: {ex.Message}", ex);
        }
    }

    public async Task<bool> ReserveStockAsync(Guid variantId, int quantity, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "inventory/reserve")
            {
                Content = JsonContent.Create(new { VariantId = variantId, Quantity = quantity })
            };
            request.Headers.Add("X-Internal-Key", GetInternalToken());

            var response = await httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.BadRequest
                || response.StatusCode == HttpStatusCode.NotFound
                || response.StatusCode == HttpStatusCode.Conflict)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<InventoryActionResponse>(cancellationToken: ct);
            return result is not null && result.Success;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Không thể kết nối đến Inventory Service để giữ chỗ tồn kho (Reserve): {ex.Message}", ex);
        }
    }

    public async Task<bool> ReleaseStockAsync(Guid variantId, int quantity, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "inventory/release")
            {
                Content = JsonContent.Create(new { VariantId = variantId, Quantity = quantity })
            };
            request.Headers.Add("X-Internal-Key", GetInternalToken());

            var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<InventoryActionResponse>(cancellationToken: ct);
                return result is not null && result.Success;
            }
            return false;
        }
        catch
        {
            // Bỏ qua lỗi nhả kho để không làm ảnh hưởng đến luồng trả lỗi chính, nhưng nên log lại
            return false;
        }
    }
}

// DTO nội bộ đại diện cho dữ liệu trả về từ Inventory Service
public record CheckStockResponse(
    Guid VariantId,
    bool IsAvailable,
    int CurrentQuantity,
    int RequiredQuantity
);

public record InventoryActionResponse(
    bool Success,
    string? Message
);