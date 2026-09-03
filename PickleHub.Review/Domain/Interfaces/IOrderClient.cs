namespace PickleHub.Review.Domain.Interfaces;

public interface IOrderClient
{
    Task<bool> VerifyOrderCompletedAsync(Guid userId, Guid orderId, Guid productId, CancellationToken ct = default);
}
