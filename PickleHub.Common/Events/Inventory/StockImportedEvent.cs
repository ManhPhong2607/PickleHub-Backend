using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickleHub.Common.Events.Inventory
{
    public record StockImportedEvent
    {
        public Guid InventoryItemId { get; init; }
        public Guid ProductVariantId { get; init; }
        public string SkuSnapshot { get; init; } = string.Empty;
        public int QuantityImported { get; init; }
        public int QuantityAfter { get; init; }
        public string? Note { get; init; }
        public Guid ImportedByUserId { get; init; }
        public string ImportedByEmail { get; init; } = string.Empty;
        public DateTime OccurredAt { get; init; }
    }

    public record StockThresholdUpdatedEvent
    {
        public Guid ProductVariantId { get; init; }
        public string SkuSnapshot { get; init; } = string.Empty;
        public int OldThreshold { get; init; }
        public int NewThreshold { get; init; }
        public Guid UpdatedByUserId { get; init; }
        public string UpdatedByEmail { get; init; } = string.Empty;
        public DateTime OccurredAt { get; init; }
    }
}
