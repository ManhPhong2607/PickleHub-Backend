using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
namespace PickleHub.Authen.Infrastructure.Persistence.Repositories
{
    public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
    {
        private readonly AuthenDbContext _db;
        public EmailVerificationTokenRepository(AuthenDbContext db)
        {
            _db = db;
        }
        public void Add(EmailVerificationToken token)
        {
            _db.EmailVerificationTokens.Add(token);
        }

        public async Task<List<EmailVerificationToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _db.EmailVerificationTokens
                .Where(t => t.UserId == userId
                    && !t.IsUsed
                    && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);
        }

        public async Task<EmailVerificationToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        {
            return await _db.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token, ct);
        }
    }
}
