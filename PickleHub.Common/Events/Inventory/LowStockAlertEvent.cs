namespace PickleHub.Common.Events.Inventory
{
    /// <summary>
    /// Phát ra khi AvailableQuantity chạm hoặc xuống dưới LowStockThreshold sau bất kỳ
    /// thao tác nào làm giảm tồn kho khả dụng (Reserve, Deduct).
    /// Khác với StockDepletedEvent (chỉ phát khi hàng về 0 sau Deduct).
    /// </summary>
    public record LowStockAlertEvent
    {
        public Guid ProductVariantId { get; init; }
        public Guid ProductId { get; init; }
        public string SkuSnapshot { get; init; } = string.Empty;
        public int AvailableQuantity { get; init; }
        public int LowStockThreshold { get; init; }
        public DateTime OccurredAt { get; init; }
    }
}
