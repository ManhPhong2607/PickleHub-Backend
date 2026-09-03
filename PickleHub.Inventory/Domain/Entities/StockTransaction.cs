using PickleHub.Common.Domain;
using PickleHub.Inventory.Domain.Enums;

namespace PickleHub.Inventory.Domain.Entities
{
    public class StockTransaction : BaseEntity
    {
        public Guid InventoryItemId { get; private set; }
        public TransactionType Type { get; private set; }
        public int Quantity { get; private set; }
        public Guid? ReferenceId { get; private set; } // OrderId
        public string? Note { get; private set; }

        private StockTransaction() { }

        public static StockTransaction Create(
            Guid inventoryItemId,
            TransactionType type,
            int quantity,
            Guid? referenceId = null,
            string? note = null
        )
        {
            return new StockTransaction
            {
                InventoryItemId = inventoryItemId,
                Type = type,
                Quantity = quantity,
                ReferenceId = referenceId,
                Note = note
            };
        }
    }
}