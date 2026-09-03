using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickleHub.Common.Events.System
{
    public record SystemConfigUpdatedEvent
    {
        public string Key { get; init; } = string.Empty;
        public string OldValue { get; init; } = string.Empty;
        public string NewValue { get; init; } = string.Empty;
        public Guid UpdatedByUserId { get; init; }
        public string UpdatedByEmail { get; init; } = string.Empty;
        public DateTime OccurredAt { get; init; }
    }
}
