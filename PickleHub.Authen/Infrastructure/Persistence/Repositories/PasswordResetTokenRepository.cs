using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Domain.Repositories;

namespace PickleHub.Authen.Infrastructure.Persistence.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AuthenDbContext _db;

        public PasswordResetTokenRepository(AuthenDbContext db)
        {
            _db = db;
        }

        public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        {
               return await _db.PasswordResetTokens
                .Include(p => p.User) 
                .FirstOrDefaultAsync(p => p.Token == token, ct);
        }
        public async Task<List<PasswordResetToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _db.PasswordResetTokens
                .Where(p => p.UserId == userId && !p.IsUsed && p.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);
        }

        public void Add(PasswordResetToken token)
        {
            _db.PasswordResetTokens.Add(token);
        }
    }
}
