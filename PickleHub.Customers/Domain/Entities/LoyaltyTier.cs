using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;

namespace PickleHub.Customers.Domain.Entities
{
    public class LoyaltyTier : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public decimal MinSpend { get; private set; }
        public decimal DiscountPercent { get; private set; }
        public int SortOrder { get; private set; }
        // Danh sách quyền lợi mô tả (JSON array of string), ví dụ:
        // ["Ưu đãi đặc quyền trong dịp sinh nhật...", "Hỗ trợ chăm sóc tận tình..."]
        // Để admin tự chỉnh nội dung hiển thị mà không cần đổi code.
        public string BenefitsJson { get; private set; } = "[]";

        private LoyaltyTier() { } 
        public static LoyaltyTier Create(string name, decimal minSpend, decimal discountPercent, int sortOrder, string benefitsJson)
        {
            if(minSpend < 0)
                throw new DomainException("Ngưỡng chi tiêu không được âm.");
            if(discountPercent < 0 || discountPercent > 100)
                throw new DomainException("Phần trăm giảm giá phải từ 0 đến 100.");

            return new LoyaltyTier
            {
                Name = name.Trim(),
                MinSpend = minSpend,
                DiscountPercent = discountPercent,
                SortOrder = sortOrder,
                BenefitsJson = string.IsNullOrWhiteSpace(benefitsJson) ? "[]" : benefitsJson
            };
        }

        public void Update(string name, decimal minSpend, decimal discountPercent, int sortOrder, string benefitsJson)
        {
            if (minSpend < 0)
                throw new DomainException("Ngưỡng chi tiêu không được âm.");
            if (discountPercent < 0 || discountPercent > 100)
                throw new DomainException("Phần trăm giảm giá phải từ 0 đến 100.");

            Name = name.Trim();
            MinSpend = minSpend;
            DiscountPercent = discountPercent;
            SortOrder = sortOrder;
            BenefitsJson = string.IsNullOrWhiteSpace(benefitsJson) ? "[]" : benefitsJson;
            SetUpdated();
        }

    }
}
