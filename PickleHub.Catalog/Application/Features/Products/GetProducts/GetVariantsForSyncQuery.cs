using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Application.Features.Products.GetProducts
{
    //Query nội bộ trả về toàn bộ variant, phục vụ Inventory đồng bộ danh sách - KHÔNG tăng ViewCount vì đây là cuộc gọi service-to-service, không phải khách xem hàng

    public record GetVariantsForSyncQuery : IRequest<List<VariantSyncDto>>;

    public class GetVariantsForSyncQueryHandler : IRequestHandler<GetVariantsForSyncQuery, List<VariantSyncDto>>
    {
        private readonly IProductRepository _productRepository;

        public GetVariantsForSyncQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<VariantSyncDto>> Handle(GetVariantsForSyncQuery request, CancellationToken ct)
        {
            var rows = await _productRepository.GetAllActiveVariantSummariesAsync(ct);
            return rows.Select(r => new VariantSyncDto
            {
                ProductId = r.ProductId,
                VariantId = r.VariantId,
                Sku = r.Sku
            }).ToList();
        }
    }
}
