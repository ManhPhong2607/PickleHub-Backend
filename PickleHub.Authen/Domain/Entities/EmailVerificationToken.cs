using PickleHub.Common.Domain;

namespace PickleHub.Authen.Domain.Entities
{
    public class EmailVerificationToken : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; } = false;
        public User User { get; private set; } = null!;
        public bool IsValid => !IsUsed && ExpiresAt > DateTime.UtcNow;

        private EmailVerificationToken() { }

        public static EmailVerificationToken Create(Guid userId, string token, int expiryMinutes = 15)
        {
            return new EmailVerificationToken
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
