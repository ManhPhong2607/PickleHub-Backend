using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Application.Features.Products.GetProducts
{
    public record GetProductsByIdsInternalQuery(List<Guid> ProductIds) : IRequest<List<ProductSummaryDto>>;

    public class GetProductsByIdsInternalHandler : IRequestHandler<GetProductsByIdsInternalQuery, List<ProductSummaryDto>>
    {
        private readonly IProductRepository _productRepository;

        public GetProductsByIdsInternalHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<ProductSummaryDto>> Handle(GetProductsByIdsInternalQuery request, CancellationToken ct)
        {
            if (request.ProductIds == null || request.ProductIds.Count == 0)
                return [];

            var products = await _productRepository.GetByIdsAsync(request.ProductIds, ct);

            return products.Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug.Value,
                ImageUrl = p.ResolveThumbnailUrl(),
                Price = p.BasePrice
            }).ToList();
        }
    }
}
