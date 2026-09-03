using MediatR;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Promotions.DeletePromotion
{
    public record DeletePromotionCommand(Guid PromotionId) : IRequest;

    public class DeletePromotionHandler : IRequestHandler<DeletePromotionCommand>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeletePromotionHandler(IPromotionRepository promotionRepository, IUnitOfWork unitOfWork)
        {
            _promotionRepository = promotionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeletePromotionCommand request, CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, ct)
                ?? throw new NotFoundException("Chương trình khuyến mãi không tồn tại.");

            // Xóa cascade toàn bộ PromotionProduct con luôn (đã cấu hình DeleteBehavior.Cascade).
            _promotionRepository.Remove(promotion);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
