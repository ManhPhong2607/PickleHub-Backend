using PickleHub.Common.Domain;

namespace PickleHub.Authen.Domain.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; } = false;
        public User User { get; private set; } = null!;
        public bool IsValid => !IsUsed && ExpiresAt > DateTime.UtcNow;
        private PasswordResetToken() { }

        public static PasswordResetToken Create(Guid userId, string token, int expiryMinutes = 15)
        {
            return new PasswordResetToken()
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
        }

        public void MarkAsUsed() 
        {
            IsUsed = true;
            SetUpdated();
        }
    }
}
