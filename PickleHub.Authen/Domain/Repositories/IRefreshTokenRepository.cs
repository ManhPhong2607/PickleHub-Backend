using PickleHub.Authen.Domain.Entities;

namespace PickleHub.Authen.Domain.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
        Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
        void Add(RefreshToken token);
    }
}
