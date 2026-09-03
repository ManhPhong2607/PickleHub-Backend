using MediatR;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Promotions.RemoveProductFromPromotion
{
   public record RemoveProductFromPromotionCommand(Guid PromotionId, List<Guid> ProductIds) : IRequest;

    public class RemoveProductFromPromotionHandler : IRequestHandler<RemoveProductFromPromotionCommand>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveProductFromPromotionHandler(IPromotionRepository promotionRepository, IUnitOfWork unitOfWork)
        {
            _promotionRepository = promotionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(RemoveProductFromPromotionCommand request, CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, ct)
                ?? throw new NotFoundException("Chương trình khuyến mãi không tồn tại.");

            foreach (var productId in request.ProductIds)
            {
                promotion.RemoveItem(productId);
            }

        //  _promotionRepository.Update(promotion);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
