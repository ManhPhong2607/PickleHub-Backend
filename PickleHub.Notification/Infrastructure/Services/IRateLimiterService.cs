namespace PickleHub.Notification.Infrastructure.Services;

public interface IRateLimiterService
{
    Task<bool> IsRateLimitedAsync(string key, int maxRequests = 5, TimeSpan? window = null);
}
