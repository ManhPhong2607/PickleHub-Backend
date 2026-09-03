using PickleHub.Common.Domain;

namespace PickleHub.Authen.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public User User { get; private set; } = null!;
        public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

        private RefreshToken() { }

        public static RefreshToken Create(Guid userId, string token, int expiryDays)
        {
            return new RefreshToken()
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
            };
        }

        public void Revoke() 
        {
            RevokedAt = DateTime.UtcNow;
            SetUpdated();
        }
    }
}
