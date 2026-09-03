using MediatR;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;
using System.Text.Json;

namespace PickleHub.Customers.Application.Features.LoyaltyTiers.GetLoyaltyTiers
{
    public record GetLoyaltyTiersQuery : IRequest<List<LoyaltyTierItemDto>>;

    public class GetLoyaltyTiersHandler : IRequestHandler<GetLoyaltyTiersQuery, List<LoyaltyTierItemDto>>
    {
        private readonly ILoyaltyTierRepository _loyaltyTierRepository;

        public GetLoyaltyTiersHandler(ILoyaltyTierRepository loyaltyTierRepository)
        {
            _loyaltyTierRepository = loyaltyTierRepository;
        }

        public async Task<List<LoyaltyTierItemDto>> Handle(GetLoyaltyTiersQuery request, CancellationToken ct)
        {
            var tiers = await _loyaltyTierRepository.GetAllOrderedAsync(ct);

            return tiers.Select(t => new LoyaltyTierItemDto
            {
                Id = t.Id,
                Name = t.Name,
                MinSpend = t.MinSpend,
                DiscountPercent = t.DiscountPercent,
                Benefits = JsonSerializer.Deserialize<List<string>>(t.BenefitsJson) ?? new()
            }).ToList();
        }
    }
}
