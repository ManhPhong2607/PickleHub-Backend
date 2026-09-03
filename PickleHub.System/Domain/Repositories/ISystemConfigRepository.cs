using PickleHub.System.Domain.Entities;

namespace PickleHub.System.Domain.Repositories
{
    public interface ISystemConfigRepository
    {
        Task<SystemConfig?> GetByKeyAsync(string key, CancellationToken ct = default);
        Task<List<SystemConfig>> GetAllAsync(CancellationToken ct = default);
        Task<bool> ExistsByKeyAsync(string key, CancellationToken ct = default);
        void Add(SystemConfig config);
        void Update(SystemConfig config);
    }
}
