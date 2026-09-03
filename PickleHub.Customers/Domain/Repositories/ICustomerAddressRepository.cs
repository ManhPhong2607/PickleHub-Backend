using PickleHub.Customers.Domain.Entities;

namespace PickleHub.Customers.Domain.Repositories
{
    public interface ICustomerAddressRepository
    {
        Task<CustomerAddress?> GetByIdAsync(Guid addressId, CancellationToken ct = default);
        Task<CustomerAddress?> GetByIdAndCustomerIdAsync(Guid addressId, Guid customerId, CancellationToken ct = default);
        Task<List<CustomerAddress>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
        Task<List<CustomerAddress>> GetDefaultsByCustomerIdAsync(Guid customerId, CancellationToken ct = default);

        void Add(CustomerAddress address);
        void Update(CustomerAddress address);
        void Remove(CustomerAddress address);
    }
}
