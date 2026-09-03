using MediatR;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;
using PickleHub.Customers.Domain.Services;

namespace PickleHub.Customers.Application.Features.Customers.GetCustomerInternal
{
    public record GetCustomerInternalQuery(Guid CustomerId) : IRequest<CustomerInternalDto?>;

    public class GetCustomerInternalHandler : IRequestHandler<GetCustomerInternalQuery, CustomerInternalDto?>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILoyaltyTierRepository _tierRepository;

        public GetCustomerInternalHandler(ICustomerRepository customerRepository, ILoyaltyTierRepository tierRepository)
        {
            _customerRepository = customerRepository;
            _tierRepository = tierRepository;
        }

        public async Task<CustomerInternalDto?> Handle(GetCustomerInternalQuery request, CancellationToken ct)
        {
            // CartOrder luôn truyền vào UserId (lấy từ JWT `sub` claim, giống hệt cách
            // Order.CustomerId/OrderCreatedEvent.CustomerId được gán xuyên suốt hệ thống) -
            // KHÔNG phải Customer.Id (khóa chính riêng, tự sinh lúc tạo Customer record,
            // khác giá trị với UserId). Phải dùng GetByUserIdAsync, không phải GetByIdAsync.
            var customer = await _customerRepository.GetByUserIdAsync(request.CustomerId, ct);
            if (customer is null) return null;

            var tiers = await _tierRepository.GetAllOrderedAsync(ct);
            var currentTier = LoyaltyTierCalculator.GetCurrentTier(customer.TotalSpent, tiers);

            return new CustomerInternalDto
            {
                Id = customer.Id,
                Email = customer.Email,
                FullName = customer.FullName,
                PhoneNumber = customer.PhoneNumber,
                LoyaltyDiscountPercent = currentTier?.DiscountPercent ?? 0
            };
        }
    }
}
