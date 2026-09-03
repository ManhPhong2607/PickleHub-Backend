namespace PickleHub.Inventory.Application.Features.DTOs
{
    public class InventoryItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid ProductId { get; set; }
        public string SkuSnapshot { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class LowStockItemDto
    {
        public Guid ProductVariantId { get; set; }
        public Guid ProductId { get; set; }
        public string SkuSnapshot { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int LowStockThreshold { get; set; }
    }

    public class CheckStockDto
    {
        public Guid VariantId { get; set; }
        public bool IsAvailable { get; set; }
        public int CurrentQuantity { get; set; }
        public int RequiredQuantity { get; set; }

        public CheckStockDto(
            Guid variantId,
            bool isAvailable,
            int currentQuantity,
            int requiredQuantity)
        {
            VariantId = variantId;
            IsAvailable = isAvailable;
            CurrentQuantity = currentQuantity;
            RequiredQuantity = requiredQuantity;
        }
    }

}
