using PickleHub.Customers.Domain.Entities ;

namespace PickleHub.Customers.Domain.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<(List<Customer> Items, int TotalItems)> GetPagedAsync(
            string? keyword,
            bool? isBlocked,
            int page,
            int pageSize,
            CancellationToken ct = default);
        Task<int> CountNewThisWeekAsync(CancellationToken ct = default);
        Task<int> CountTotalAsync(CancellationToken ct = default);
        Task<int> CountBlockedAsync(CancellationToken ct = default);

        void Add(Customer customer);
        void Update(Customer customer);
    }
}
