using MediatR;
using PickleHub.Catalog.Application.Features.Promotions.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.Exceptions;

namespace PickleHub.Catalog.Application.Features.Promotions.GetPromotionById
{
    public record GetPromotionByIdQuery(Guid PromotionId) : IRequest<PromotionDto>;

    public class GetPromotionByIdHandler : IRequestHandler<GetPromotionByIdQuery, PromotionDto>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IProductRepository _productRepository;

        public GetPromotionByIdHandler(IPromotionRepository promotionRepository, IProductRepository productRepository)
        {
            _promotionRepository = promotionRepository;
            _productRepository = productRepository;
        }

        public async Task<PromotionDto> Handle(GetPromotionByIdQuery request, CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, ct)
                ?? throw new NotFoundException("Chương trình khuyến mãi không tồn tại.");

            return await promotion.MapToDtoAsync(_productRepository, ct);
        }
    }
}
