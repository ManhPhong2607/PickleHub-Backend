using MediatR;
using PickleHub.Catalog.Application.Features.Promotions.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Promotions.AddProductsToPromotion
{
    public record AddProductsToPromotionCommand(
        Guid PromotionId,
        List<PromotionItemInput> Items
    ) : IRequest<AssignProductsResultDto>;

    public class AddProductsToPromotionHandler : IRequestHandler<AddProductsToPromotionCommand, AssignProductsResultDto>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddProductsToPromotionHandler(
            IPromotionRepository promotionRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork)
        {
            _promotionRepository = promotionRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<AssignProductsResultDto> Handle(AddProductsToPromotionCommand request, CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, ct)
                 ?? throw new NotFoundException("Chương trình khuyến mãi không tồn tại.");

            var conflictingIds = new HashSet<Guid>();
            if (request.Items.Count > 0)
            {
                var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
                // excludePromotionId = chính Promotion này - sản phẩm đã nằm trong Promotion này rồi (đang update lại % giảm) không tính là "xung đột với chính mình".
                conflictingIds = await _promotionRepository.GetConflictingProductIdsAsync(
                    productIds, promotion.StartsAt, promotion.EndsAt, promotion.Priority, promotion.Id, ct);

                foreach (var item in request.Items)
                {
                    if (conflictingIds.Contains(item.ProductId)) continue;
                    promotion.AddOrUpdateItem(item.ProductId, item.DiscountPercent);
                }
            }
            await _unitOfWork.SaveChangesAsync(ct);

            var dto = await promotion.MapToDtoAsync(_productRepository, ct);

            return new AssignProductsResultDto
            {
                Promotion = dto,
                // Đếm theo số item request success (không bị conflict) - không dựa vào chênh lệch Count, vì item update % cho sản phẩm đã có sẵn không làm tăng Countnhưng vẫn là 1 thao tác success.
                SuccessCount = request.Items.Count - conflictingIds.Count,
                ConflictingProductIds = conflictingIds.ToList()
            };
        }
    }
}
