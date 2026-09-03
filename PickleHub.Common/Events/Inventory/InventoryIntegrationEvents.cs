using System;

namespace PickleHub.Common.Events.Inventory
{
    public record InventoryReservedEvent
    {
        public Guid ProductVariantId { get; init; }
        public Guid ProductId { get; init; }
        public int ReservedQuantity { get; init; }
        public int AvailableQuantity { get; init; }
        public int PhysicalQuantity { get; init; }
        public string? ReferenceId { get; init; }
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }

    public record InventoryReleasedEvent
    {
        public Guid ProductVariantId { get; init; }
        public Guid ProductId { get; init; }
        public int ReleasedQuantity { get; init; }
        public int AvailableQuantity { get; init; }
        public int PhysicalQuantity { get; init; }
        public string? ReferenceId { get; init; }
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }

    public record InventoryAdjustedEvent
    {
        public Guid ProductVariantId { get; init; }
        public Guid ProductId { get; init; }
        public int PreviousPhysical { get; init; }
        public int NewPhysical { get; init; }
        public int Change { get; init; }
        public string Reason { get; init; } = string.Empty;
        public string AdjustedBy { get; init; } = "Admin";
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }

    public record InventoryThresholdCrossedEvent
    {
        public Guid ProductVariantId { get; init; }
        public Guid ProductId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public string ProductName { get; init; } = string.Empty;
        public int CurrentAvailableQuantity { get; init; }
        public int Threshold { get; init; }
        public bool IsDepleted { get; init; }
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }
}
