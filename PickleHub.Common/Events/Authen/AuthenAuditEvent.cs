using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickleHub.Common.Events.Authen
{
    public record UserLoggedInEvent
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public DateTime OccurredAt { get; init; }
    }

    public record UserPasswordChangedEvent
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty; // "Changed" | "Reset"
        public DateTime OccurredAt { get; init; }
    }
}
