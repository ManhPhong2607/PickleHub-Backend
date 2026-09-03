using PickleHub.Common.Domain;

namespace PickleHub.Customers.Domain.Entities
{
    public class Customer : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public string? PhoneNumber { get; private set; }
        public string? AvatarUrl { get; private set; }
        public bool IsBlocked { get; private set; } = false;
        public decimal TotalSpent { get; private set; } = 0;

        private readonly List<CustomerAddress> _addresses = new();
        public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

        private Customer() { }

        public static Customer Create(Guid userId, string email, string fullName)
        {
            return new Customer
            {
                UserId = userId,
                Email = email.Trim().ToLowerInvariant(),
                FullName = fullName.Trim()
            };
        }

        public void Update(string fullName, string? phoneNumber)
        {
            FullName = fullName.Trim();
            PhoneNumber = phoneNumber?.Trim();
            SetUpdated();
        }

        public void UpdateAvatar(string? avatarUrl)
        {
            AvatarUrl = avatarUrl;
            SetUpdated();
        }

        public void Block()
        {
            IsBlocked = true;
            SetUpdated();
        }

        public void Unblock()
        {
            IsBlocked = false;
            SetUpdated();
        }

        public void AddSpend(decimal amount)
        {
            if (amount <= 0)
                return;
            TotalSpent += amount;
            SetUpdated();
        }
    }
}
