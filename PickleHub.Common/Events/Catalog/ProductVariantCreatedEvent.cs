using System;

namespace PickleHub.Common.Events.Catalog
{
    public record ProductVariantCreatedEvent
    {
        public Guid VariantId { get; init; }
        public Guid ProductId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }
}
