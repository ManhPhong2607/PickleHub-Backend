using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Customers.Domain.Repositories;

namespace PickleHub.Customers.Application.Features.LoyaltyTiers.DeleteLoyaltyTier
{
    public record DeleteLoyaltyTierCommand(Guid Id) : IRequest;

    public class DeleteLoyaltyTierHandler : IRequestHandler<DeleteLoyaltyTierCommand>
    {
        private readonly ILoyaltyTierRepository _loyaltyTierRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteLoyaltyTierHandler(ILoyaltyTierRepository loyaltyTierRepository, IUnitOfWork unitOfWork)
        {
            _loyaltyTierRepository = loyaltyTierRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(DeleteLoyaltyTierCommand request, CancellationToken ct)
        {
            var tier = await _loyaltyTierRepository.GetByIdAsync(request.Id, ct)
                ?? throw new NotFoundException("Hạng thành viên không tồn tại.");

            _loyaltyTierRepository.Remove(tier);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
