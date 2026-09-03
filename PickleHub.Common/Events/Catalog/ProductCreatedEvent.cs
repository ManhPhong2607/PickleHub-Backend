using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickleHub.Common.Events.Catalog
{
    public record ProductCreatedEvent
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public string BrandName { get; init; } = string.Empty;
        // BasePrice không được include ở đây vì product mới tạo chưa có variant -> giá chỉ có ý nghĩa sau khi AddVariant được gọi.
        public Guid CreatedByUserId { get; init; }
        public string CreatedByEmail { get; init; } = string.Empty;
        public DateTime OccurredAt { get; init; }
    }

    public record ProductUpdatedEvent
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public Guid UpdatedByUserId { get; init; }
        public string UpdatedByEmail { get; init; } = string.Empty;
        public DateTime OccurredAt { get; init; }
    }

    public record ProductStatusChangedEvent
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty; // "Published" | "Hidden" | "Restored"
        public Guid ActorUserId { get; init; }
        public string ActorEmail { get; init; } = string.Empty;
        public DateTime OccurredAt { get; init; }
    }
}
