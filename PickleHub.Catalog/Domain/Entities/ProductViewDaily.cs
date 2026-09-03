using PickleHub.Common.Domain;

namespace PickleHub.Catalog.Domain.Entities
{
    public class ProductViewDaily : BaseEntity
    {
        public Guid ProductId { get; private set; }
        public DateOnly ViewDate { get; private set; }
        public int ViewCount { get; private set; }

        private ProductViewDaily() { }

        public static ProductViewDaily Create(Guid productId, DateOnly viewDate)
        {
            return new ProductViewDaily
            {
                ProductId = productId,
                ViewDate = viewDate,
                ViewCount = 0
            };
        }

        public void IncrementView()
        {
            ViewCount++;
        }
    }
}
