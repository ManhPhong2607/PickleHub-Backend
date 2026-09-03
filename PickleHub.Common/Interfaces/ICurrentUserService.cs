namespace PickleHub.Common.Interfaces
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string Role { get; }
        bool IsAuthenticated { get; }
        string? Email { get; }
    }
}
