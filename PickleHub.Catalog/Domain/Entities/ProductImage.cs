using PickleHub.Common.Domain;

namespace PickleHub.Catalog.Domain.Entities
{
    public class ProductImage : BaseEntity
    {
        public Guid ProductId { get; private set; }
        public Guid? VariantId { get; private set; }
        public string PublicId { get; private set; } = string.Empty; 
        public string Url { get; private set; } = string.Empty;
        public int SortOrder { get; private set; }
        public Product? Product { get; private set; }
        public bool IsSizeChart { get; private set; }

        private ProductImage() { }
        public static ProductImage Create(
              Guid productId,
              string publicId,
              string url,
              int sortOrder,
              Guid? variantId = null,
              bool isSizeChart = false)
        {
            return new ProductImage
            {
                ProductId = productId,
                PublicId = publicId,
                Url = url,
                SortOrder = sortOrder,
                VariantId = variantId,
                IsSizeChart = isSizeChart
            };
        }
    }
}
