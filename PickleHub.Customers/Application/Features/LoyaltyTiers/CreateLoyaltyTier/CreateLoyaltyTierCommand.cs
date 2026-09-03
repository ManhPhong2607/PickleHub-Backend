using MediatR;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Entities;
using PickleHub.Customers.Domain.Repositories;
using System.Text.Json;

namespace PickleHub.Customers.Application.Features.LoyaltyTiers.CreateLoyaltyTier
{
    public record CreateLoyaltyTierCommand(
        string Name,
        decimal Minspend,
        decimal DiscountPercent, 
        int SortOrder,
        string BenefitsJson
        ) : IRequest<LoyaltyTierItemDto>;

    public class CreateLoyaltyTierCommandHandler : IRequestHandler<CreateLoyaltyTierCommand, LoyaltyTierItemDto>
    {
        private readonly ILoyaltyTierRepository _loyaltyTierRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateLoyaltyTierCommandHandler(ILoyaltyTierRepository loyaltyTierRepository, IUnitOfWork unitOfWork)
        {
            _loyaltyTierRepository = loyaltyTierRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<LoyaltyTierItemDto> Handle(CreateLoyaltyTierCommand request, CancellationToken ct)
        {
            var tier = LoyaltyTier.Create(request.Name, request.Minspend, request.DiscountPercent, request.SortOrder, request.BenefitsJson);
            _loyaltyTierRepository.Add(tier);
            await _unitOfWork.SaveChangesAsync(ct);

            return new LoyaltyTierItemDto
            {
                Id = tier.Id,
                Name = tier.Name,
                MinSpend = tier.MinSpend,
                DiscountPercent = tier.DiscountPercent,
                Benefits = JsonSerializer.Deserialize<List<string>>(tier.BenefitsJson) ?? new(),
                IsCurrentTier = false,
                IsAchieved = false
            };
        }
    }
}
