using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Domain.Entities
{
    public class PromotionProduct : BaseEntity
    {
        public Guid PromotionId { get; private set; }
        public Guid ProductId { get; private set; }
        public decimal DiscountPercent { get; private set; }
        private PromotionProduct() { }

        public static PromotionProduct Create(Guid promotionId, Guid productId, decimal discountPercent)
        {
            if(discountPercent <= 0 || discountPercent >= 100)
            {
                throw new DomainException("Phần trăm giảm giá phải trong khoảng 0-100 (không bao gồm 2 đầu mút).");
            }
            return new PromotionProduct
            {
                PromotionId = promotionId,
                ProductId = productId,
                DiscountPercent = discountPercent
            };
        }

        public void UpdateDiscount(decimal disccountPercent) 
        {
            if(disccountPercent <= 0 || disccountPercent >= 100)
            {
                throw new DomainException("Phần trăm giảm giá phải trong khoảng 0-100 (không bao gồm 2 đầu mút).");
            }
            DiscountPercent = disccountPercent;
            SetUpdated();
        }
    }
}
