namespace PickleHub.Inventory.Application.Settings
{
    public class InventorySettings
    {
        public const string SectionName = "Inventory";

        public int DefaultLowStockThreshold { get; init; } = 5;
    }
}
