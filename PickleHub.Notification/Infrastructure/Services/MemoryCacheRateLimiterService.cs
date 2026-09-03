using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace PickleHub.Notification.Infrastructure.Services;

public class MemoryCacheRateLimiterService(IMemoryCache cache, ILogger<MemoryCacheRateLimiterService> logger) : IRateLimiterService
{
    public Task<bool> IsRateLimitedAsync(string key, int maxRequests = 5, TimeSpan? window = null)
    {
        var timeWindow = window ?? TimeSpan.FromMinutes(1);
        var cacheKey = $"RateLimit_{key}";

        lock (cache)
        {
            if (!cache.TryGetValue(cacheKey, out int currentCount))
            {
                currentCount = 0;
            }

            if (currentCount >= maxRequests)
            {
                logger.LogWarning("Rate Limit Triggered! Key '{Key}' vượt quá giới hạn {MaxRequests} emails / {Window}s", key, maxRequests, timeWindow.TotalSeconds);
                return Task.FromResult(true); // bị Rate Limited
            }

            currentCount++;

            cache.Set(cacheKey, currentCount, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeWindow
            });

            return Task.FromResult(false); // hợp lệ
        }
    }
}
