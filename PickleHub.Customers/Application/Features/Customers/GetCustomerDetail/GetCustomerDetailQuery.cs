using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.Customers.GetCustomerDetail
{
    public record GetCustomerDetailQuery(Guid CustomerId) : IRequest<CustomerDto>;

    public class GetCustomerDetailHandler : IRequestHandler<GetCustomerDetailQuery, CustomerDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILoyaltyTierRepository _loyaltyTierRepository;

        public GetCustomerDetailHandler(
            ICustomerRepository customerRepository,
            ILoyaltyTierRepository loyaltyTierRepository)
        {
            _customerRepository = customerRepository;
            _loyaltyTierRepository = loyaltyTierRepository;
        }

        public async Task<CustomerDto> Handle(GetCustomerDetailQuery request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, ct)
                ?? await _customerRepository.GetByUserIdAsync(request.CustomerId, ct)
                ?? throw new NotFoundException("Không tìm thấy khách hàng.");

            var tiers = await _loyaltyTierRepository.GetAllOrderedAsync(ct);
            var currentTier = Domain.Services.LoyaltyTierCalculator.GetCurrentTier(customer.TotalSpent, tiers);
            var nextTier = Domain.Services.LoyaltyTierCalculator.GetNextTier(customer.TotalSpent, tiers);

            return new CustomerDto
            {
                Id = customer.Id,
                UserId = customer.UserId,
                Email = customer.Email,
                FullName = customer.FullName,
                PhoneNumber = customer.PhoneNumber,
                AvatarUrl = customer.AvatarUrl,
                IsBlocked = customer.IsBlocked,
                TotalSpent = customer.TotalSpent,
                CurrentTierName = currentTier?.Name ?? "Thành viên mới",
                CurrentDiscountPercent = currentTier?.DiscountPercent ?? 0,
                NextTierName = nextTier?.Name,
                NextTierMinSpend = nextTier?.MinSpend,
                AmountNeededForNextTier = nextTier != null ? nextTier.MinSpend - customer.TotalSpent : null,
                Addresses = customer.Addresses.Select(a => new AddressDto
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    PhoneNumber = a.PhoneNumber,
                    Province = a.Province,
                    District = a.District,
                    Ward = a.Ward,
                    StreetAddress = a.StreetAddress,
                    IsDefault = a.IsDefault
                }).ToList(),
                CreatedAt = customer.CreatedAt
            };
        }
    }
}
