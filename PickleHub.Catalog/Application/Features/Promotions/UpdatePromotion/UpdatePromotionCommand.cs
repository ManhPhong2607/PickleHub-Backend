using MediatR;
using PickleHub.Catalog.Application.Features.Promotions.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Promotions.UpdatePromotion
{
    public record UpdatePromotionCommand(
        Guid PromotionId,
        string Name,
        string? Description,
        DateTime StartsAt,
        DateTime EndsAt,
        bool IsActive,
        int Priority 
    ) : IRequest<PromotionDto>;

    public class UpdatePromotionHandler : IRequestHandler<UpdatePromotionCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdatePromotionHandler(
            IPromotionRepository promotionRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork)
        {
            _promotionRepository = promotionRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> Handle(UpdatePromotionCommand request, CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, ct)
                ?? throw new NotFoundException("Chương trình khuyến mãi không tồn tại.");

            var dateChanged = promotion.StartsAt != request.StartsAt || promotion.EndsAt != request.EndsAt;
            var priorityChanged = promotion.Priority != request.Priority;
            var willBeActive = request.IsActive;

            // Chỉ cần re-check overlap khi ngày HOẶC priority thay đổi VÀ promotion sẽ active -
            // vì priority đổi cũng có thể khiến nó rơi vào (hoặc thoát khỏi) trường hợp trùng priority với 1 Promotion khác đang overlap ngày.
            if ((dateChanged || priorityChanged) && willBeActive && promotion.Items.Count > 0)
            {
                var productIds = promotion.Items.Select(i => i.ProductId).Distinct().ToList();

                var conflictingIds = await _promotionRepository.GetConflictingProductIdsAsync(
                    productIds, request.StartsAt, request.EndsAt, request.Priority, promotion.Id, ct);

                if (conflictingIds.Count > 0)
                {
                    throw new ConflictException(
                        $"Không thể lưu vì {conflictingIds.Count} sản phẩm trong chương trình này " +
                        "sẽ bị trùng cả thời gian LẪN độ ưu tiên với 1 chương trình khuyến mãi khác đang chạy. " +
                        "Vui lòng gỡ các sản phẩm đó, hoặc đổi độ ưu tiên khác trước khi lưu.");
                }
            }

            promotion.UpdateInfo(request.Name, request.Description, request.StartsAt, request.EndsAt, request.IsActive, request.Priority);
            await _unitOfWork.SaveChangesAsync(ct);

            return await promotion.MapToDtoAsync(_productRepository, ct);
        }
    }
}
