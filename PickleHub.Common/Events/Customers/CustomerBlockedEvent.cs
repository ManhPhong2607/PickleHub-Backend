using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickleHub.Common.Events.Customers
{
    public record CustomerBlockedEvent
    {
        public Guid CustomerId { get; init; }
        public Guid UserId { get; init; }
        public string CustomerEmail { get; init; } = string.Empty;
        public bool IsBlocked { get; init; }
        public DateTime OccurredAt { get; init; }
    }
}
