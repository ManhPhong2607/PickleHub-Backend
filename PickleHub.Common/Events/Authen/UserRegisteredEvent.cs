using System;

namespace PickleHub.Common.Events.Authen;

public record UserRegisteredEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string VerificationToken { get; init; } = string.Empty;
    public string VerificationUrl { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
}
