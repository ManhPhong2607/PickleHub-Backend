using PickleHub.Authen.Domain.Entities;

namespace PickleHub.Authen.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> AnyAdminAsync(CancellationToken ct = default);
        void Add(User user);
        void Update(User user);
    }
}
