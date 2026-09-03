using PickleHub.Common.Domain;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Domain.Entities
{
    public class ProductVariant : BaseEntity
    {
        public Guid ProductId { get; private set; }
        public string Sku { get; private set; } = string.Empty;
        public string AttributesJson { get; private set; } = "{}";
        public decimal Price { get; private set; }
        public Product? Product { get; private set; }

        private ProductVariant() { }
        public static ProductVariant Create(Guid productId, string sku,
            string attributesJson, decimal price)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new DomainException("SKU không được để trống.");

            if (price <= 0)
                throw new DomainException("Giá biến thể phải lớn hơn 0.");

            return new ProductVariant
            {
                ProductId = productId,
                Sku = sku,
                AttributesJson = string.IsNullOrWhiteSpace(attributesJson) ? "{}" : attributesJson,
                Price = price
            };
        }

        public void Update(string sku, string attributesJson, decimal price)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new DomainException("SKU không được để trống.");

            if (price <= 0)
                throw new DomainException("Giá biến thể phải lớn hơn 0.");

            Sku = sku;
            AttributesJson = string.IsNullOrWhiteSpace(attributesJson) ? "{}" : attributesJson;
            Price = price;
            SetUpdated();
        }
    }
}
