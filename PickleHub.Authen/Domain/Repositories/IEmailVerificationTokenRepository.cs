using PickleHub.Authen.Domain.Entities;

namespace PickleHub.Authen.Domain.Repositories
{
    public interface IEmailVerificationTokenRepository
    {
        Task<EmailVerificationToken?> GetByTokenAsync(string token, CancellationToken ct = default);
        Task<List<EmailVerificationToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
        void Add(EmailVerificationToken token);
    }
}
