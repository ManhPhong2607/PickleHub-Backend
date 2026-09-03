using System;

namespace PickleHub.Common.Events.Authen;

public record PasswordResetRequestedEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ResetToken { get; init; } = string.Empty;
    public string ResetUrl { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
}
