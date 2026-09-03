using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;
using PickleHub.Customers.Domain.Services;
using System.Text.Json;

namespace PickleHub.Customers.Application.Features.Customers.GetLoyalty
{
    public record GetLoyaltyQuery : IRequest<LoyaltyDto>;

    public class GetLoyaltyHandler : IRequestHandler<GetLoyaltyQuery, LoyaltyDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILoyaltyTierRepository _loyaltyTierRepository;
        private readonly ICurrentUserService _currentUser;
        public GetLoyaltyHandler(ICustomerRepository customerRepository, ILoyaltyTierRepository loyaltyTierRepository, ICurrentUserService currentUser)
        {
            _customerRepository = customerRepository;
            _loyaltyTierRepository = loyaltyTierRepository;
            _currentUser = currentUser;
        }

        public async Task<LoyaltyDto> Handle(GetLoyaltyQuery request, CancellationToken ct)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin khách hàng.");

            var tiers = await _loyaltyTierRepository.GetAllOrderedAsync(ct); // đã sort theo sortOrder tăng dần

            // hạng hiện tại: hạng có MinSpend cao nhất mà TotalSpend vẫn đạt được
            var currentTier = LoyaltyTierCalculator.GetCurrentTier(customer.TotalSpent, tiers);

            //hạng tiếp theo: hạng có MinSpend thấp nhất trong số các hạng khách chưa đạt được
            var nextTier = LoyaltyTierCalculator.GetNextTier(customer.TotalSpent, tiers);

            return new LoyaltyDto
            {
                TotalSpent = customer.TotalSpent,
                CurrentTierName = currentTier?.Name,
                CurrentDiscountPercent = currentTier?.DiscountPercent ?? 0,
                NextTierName = nextTier?.Name,
                NextTierMinSpend = nextTier?.MinSpend,
                AmountNeededForNextTier = nextTier != null ? nextTier.MinSpend - customer.TotalSpent : null,
                AllTiers = tiers.Select(t => new LoyaltyTierItemDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    MinSpend = t.MinSpend,
                    DiscountPercent = t.DiscountPercent,
                    Benefits = JsonSerializer.Deserialize<List<string>>(t.BenefitsJson) ?? new List<string>(),
                    IsCurrentTier = currentTier != null && t.Id == currentTier.Id,
                    IsAchieved = customer.TotalSpent >= t.MinSpend
                }).ToList()
            };
        }
    }
}
