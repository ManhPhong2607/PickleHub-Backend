using System.Text.Json.Serialization;

namespace PickleHub.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1,
    Failed = 2
}
