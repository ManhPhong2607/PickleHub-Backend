using MediatR;
using PickleHub.Catalog.Application.Features.Promotions.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Entities;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Interfaces;

namespace PickleHub.Catalog.Application.Features.Promotions.CreatePromotion
{
    public record CreatePromotionCommand(
         string Name,
         string? Description,
         DateTime StartsAt,
         DateTime EndsAt,
         bool IsActive,
         int Priority = 0,
         List<PromotionItemInput>? Items = null) : IRequest<AssignProductsResultDto>;

    public class CreatePromotionHandler : IRequestHandler<CreatePromotionCommand, AssignProductsResultDto>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePromotionHandler(
            IPromotionRepository promotionRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork)
        {
            _promotionRepository = promotionRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AssignProductsResultDto> Handle(CreatePromotionCommand request, CancellationToken ct)
        {
            var promotion = Promotion.Create(request.Name, request.Description, request.StartsAt, request.EndsAt, request.IsActive, request.Priority);

            var conflictingIds = new HashSet<Guid>();

            if (request.Items != null && request.Items.Count > 0)
            {
                var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

                conflictingIds = await _promotionRepository.GetConflictingProductIdsAsync(
                    productIds, request.StartsAt, request.EndsAt, request.Priority, promotionIdToExclude: null, ct);

                foreach (var item in request.Items)
                {
                    if (conflictingIds.Contains(item.ProductId)) continue;
                    promotion.AddOrUpdateItem(item.ProductId, item.DiscountPercent);
                }
            }

            _promotionRepository.Add(promotion);
            await _unitOfWork.SaveChangesAsync(ct);

            var dto = await promotion.MapToDtoAsync(_productRepository, ct);

            return new AssignProductsResultDto
            {
                Promotion = dto,
                SuccessCount = request.Items.Count - conflictingIds.Count,
                ConflictingProductIds = conflictingIds.ToList()
            };
        }
    }
}
