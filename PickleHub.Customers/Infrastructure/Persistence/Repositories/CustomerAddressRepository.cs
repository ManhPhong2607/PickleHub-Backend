using Microsoft.EntityFrameworkCore;
using PickleHub.Customers.Domain.Entities;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Infrastructure.Persistence.Repositories
{
    public class CustomerAddressRepository : ICustomerAddressRepository
    {
        private readonly CustomerDbContext _db;

        public CustomerAddressRepository(CustomerDbContext db)
        {
            _db = db;
        }

        public async Task<CustomerAddress?> GetByIdAsync(Guid addressId, CancellationToken ct = default)
        {
            return await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == addressId, ct);
        }

        public async Task<CustomerAddress?> GetByIdAndCustomerIdAsync(
            Guid addressId,
            Guid customerId,
            CancellationToken ct = default)
        {
            return await _db.CustomerAddresses.FirstOrDefaultAsync(
                a => a.Id == addressId && a.CustomerId == customerId, ct);
        }

        public async Task<List<CustomerAddress>> GetByCustomerIdAsync(
            Guid customerId,
            CancellationToken ct = default)
        {
            return await _db.CustomerAddresses
                .AsNoTracking()
                .Where(a => a.CustomerId == customerId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<CustomerAddress>> GetDefaultsByCustomerIdAsync(
            Guid customerId,
            CancellationToken ct = default)
        {
            return await _db.CustomerAddresses
                .Where(a => a.CustomerId == customerId && a.IsDefault)
                .ToListAsync(ct);
        }

        public void Add(CustomerAddress address)
        {
            _db.CustomerAddresses.Add(address);
        }

        public void Update(CustomerAddress address)
        {
            _db.CustomerAddresses.Update(address);
        }

        public void Remove(CustomerAddress address)
        {
            _db.CustomerAddresses.Remove(address);
        }
    }
}
