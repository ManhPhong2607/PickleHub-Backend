using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace PickleHub.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipping = 2,
    Completed = 3,
    Cancelled = 4
}
