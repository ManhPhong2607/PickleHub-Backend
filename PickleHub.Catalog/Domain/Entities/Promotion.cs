using PickleHub.Catalog.Domain.Enums;
using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Domain.Entities
{
    public class Promotion : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; } 
        public DateTime StartsAt { get; private set; }
        public DateTime EndsAt { get; private set; }
        public bool IsActive { get; private set; } = true;
        public int Priority { get; private set; } = 0;

        private readonly List<PromotionProduct> _items = new();
        public IReadOnlyCollection<PromotionProduct> Items => _items.AsReadOnly();

        public bool IsCurrentlyRunning => IsActive && DateTime.UtcNow >= StartsAt && DateTime.UtcNow <= EndsAt;

        public PromotionStatus Status
        {
            get
            {
                if (!IsActive) return PromotionStatus.Disabled;
                
                var now = DateTime.UtcNow;
                if (now < StartsAt) return PromotionStatus.Scheduled;
                if (now > EndsAt) return PromotionStatus.Expired;
                
                return PromotionStatus.Active;
            }
        }

        public static Promotion Create(string name, string? description, DateTime startsAt, DateTime endsAt, bool isActive = true, int priority = 0)
        {
            ValidateDateRange(startsAt, endsAt);

            return new Promotion
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                StartsAt = DateTime.SpecifyKind(startsAt, DateTimeKind.Utc),
                EndsAt = DateTime.SpecifyKind(endsAt, DateTimeKind.Utc),
                IsActive = isActive,
                Priority = priority
            };
        }

        public void UpdateInfo(string name, string? description, DateTime startsAt, DateTime endsAt, bool isActive, int priority)
        {
            ValidateDateRange(startsAt, endsAt);

            Name = name.Trim();
            Description = description?.Trim();
            StartsAt = DateTime.SpecifyKind(startsAt, DateTimeKind.Utc);
            EndsAt = DateTime.SpecifyKind(endsAt, DateTimeKind.Utc);
            IsActive = isActive;
            Priority = priority;
            SetUpdated();
        }

        // Thêm mới hoặc cập nhật % giảm nếu ProductId đã có trong Promotion này rồi -
        // KHÔNG check overlap ở đây (overlap là kiểm tra CHÉO giữa các Promotion khác nhau)
        public void AddOrUpdateItem(Guid productId, decimal discountPercent)
        {
            if (discountPercent <= 0 || discountPercent >= 100)
                throw new DomainException("Phần trăm giảm giá phải trong khoảng 0-100 (không bao gồm 2 đầu mút).");

            var existing = _items.FirstOrDefault(i => i.ProductId == productId);
            if (existing != null)
            {
                existing.UpdateDiscount(discountPercent);
            }
            else
            {
                _items.Add(PromotionProduct.Create(Id, productId, discountPercent));
            }

            SetUpdated();
        }

        public void RemoveItem(Guid productId)
        {
            var item = _items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                throw new NotFoundException("Sản phẩm này không nằm trong chương trình khuyến mãi.");

            _items.Remove(item);
            SetUpdated();
        }

        private static void ValidateDateRange(DateTime startsAt, DateTime endsAt)
        {
            if (endsAt <= startsAt)
                throw new DomainException("Ngày kết thúc phải sau ngày bắt đầu.");
        }
    }
}
