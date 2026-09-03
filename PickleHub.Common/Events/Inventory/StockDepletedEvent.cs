using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickleHub.Common.Events.Inventory
{
    public record StockDepletedEvent
    {
        public Guid VariantId { get; init; }

        // OrderId da duocc xác nhan 
        public Guid ConfirmedOrderId { get; init; }

        public DateTime OccurredAt { get; init; }
    }
}
