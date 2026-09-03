using Microsoft.EntityFrameworkCore;
using PickleHub.Customers.Domain.Entities;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Infrastructure.Persistence.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly CustomerDbContext _db;

        public CustomerRepository(CustomerDbContext db) 
        {
            _db = db;
        }

        public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _db.Customers.AnyAsync(c => c.UserId == userId, ct);
        }

        public async Task<(List<Customer> Items, int TotalItems)> GetPagedAsync(string? keyword, bool? isBlocked, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(c=> c.FullName.Contains(keyword) || c.Email.Contains(keyword));
            }

            if (isBlocked == true) 
            {
                query = query.Where(c=> c.IsBlocked == isBlocked.Value);
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(c=> c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            return(items, total);
        }

        public async Task<int> CountNewThisWeekAsync(CancellationToken ct = default)
        {
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            return await _db.Customers.CountAsync(c => c.CreatedAt >= weekAgo, ct);
        }

        public async Task<int> CountTotalAsync(CancellationToken ct = default)
        {
            return await _db.Customers.CountAsync(ct);
        }

        public async Task<int> CountBlockedAsync(CancellationToken ct = default)
        {
            return await _db.Customers.CountAsync(c => c.IsBlocked, ct);
        }

        public void Add(Customer customer)
        {
            _db.Customers.Add(customer);
        }

        public void Update(Customer customer)
        {
            _db.Customers.Update(customer);
        }
    }
}
