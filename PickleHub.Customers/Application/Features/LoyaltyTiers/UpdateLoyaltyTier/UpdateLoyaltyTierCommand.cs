using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Application.Features.DTOs;
using PickleHub.Customers.Domain.Repositories;
using System.Text.Json;

namespace PickleHub.Customers.Application.Features.LoyaltyTiers.UpdateLoyaltyTier
{
    public record UpdateLoyaltyTierCommand(
        Guid Id,
        string Name,
        decimal MinSpend,
        decimal DiscountPercent,
        int SortOrder,
        string BenefitsJson
    ) : IRequest<LoyaltyTierItemDto>;

    public class UpdateLoyaltyTierCommandHandler : IRequestHandler<UpdateLoyaltyTierCommand, LoyaltyTierItemDto>
    {
        private readonly ILoyaltyTierRepository _loyaltyTierRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateLoyaltyTierCommandHandler(ILoyaltyTierRepository loyaltyTierRepository, IUnitOfWork unitOfWork)
        {
            _loyaltyTierRepository = loyaltyTierRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<LoyaltyTierItemDto> Handle(UpdateLoyaltyTierCommand request, CancellationToken ct)
        {
            var tier = await _loyaltyTierRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Hạng thành viên không tồn tại.");

            tier.Update(request.Name, request.MinSpend, request.DiscountPercent, request.SortOrder, request.BenefitsJson);
            _loyaltyTierRepository.Update(tier);
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
