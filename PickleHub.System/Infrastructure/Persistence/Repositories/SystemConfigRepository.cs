using Microsoft.EntityFrameworkCore;
using PickleHub.System.Domain.Entities;
using PickleHub.System.Domain.Repositories;

namespace PickleHub.System.Infrastructure.Persistence.Repositories
{
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly SystemDbContext _db;
        public SystemConfigRepository(SystemDbContext db)
        {
            _db = db;
        }

        public async Task<SystemConfig?> GetByKeyAsync(string key, CancellationToken ct = default)
        {
            return await _db.SystemConfigs
                .FirstOrDefaultAsync(c => c.Key == key.Trim().ToLowerInvariant(), ct);
        }

        public async Task<List<SystemConfig>> GetAllAsync(CancellationToken ct = default)
        {
           return await _db.SystemConfigs
                .AsNoTracking()
                .OrderBy(c => c.Key)
                .ToListAsync(ct);
        }
        public async Task<bool> ExistsByKeyAsync(string key, CancellationToken ct = default)
        {
           return await _db.SystemConfigs
                .AnyAsync(c => c.Key == key.Trim().ToLowerInvariant(), ct);
        }
        public void Add(SystemConfig config)
        {
            _db.SystemConfigs.Add(config);
        }
        public void Update(SystemConfig config)
        {
            _db.SystemConfigs.Update(config);
        }
    }
}
