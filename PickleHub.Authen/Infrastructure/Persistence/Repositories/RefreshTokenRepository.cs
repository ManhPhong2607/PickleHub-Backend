using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Domain.Repositories;

namespace PickleHub.Authen.Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AuthenDbContext _db;

        public RefreshTokenRepository(AuthenDbContext db)
        {
            _db = db;
        }
        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        {
             return await _db.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == token, ct);
        }
           

        public async Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
              return await _db.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);
        }

        public void Add(RefreshToken token)
        {
             _db.RefreshTokens.Add(token);
        }
    }
}
