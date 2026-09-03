using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Application.Features.Products.GetProducts
{
    public record GetProductInsightsQuery : IRequest<ProductInsightsResultDto>;

    public class GetProductInsightsQueryHandler : IRequestHandler<GetProductInsightsQuery, ProductInsightsResultDto>
    {
        private readonly IProductRepository _productRepository;
        // Sản phẩm cần đạt tối thiểu số view này mới được xếp loại 
        private const int MinViewsToClassify = 5; 
        public GetProductInsightsQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ProductInsightsResultDto> Handle(GetProductInsightsQuery request, CancellationToken ct)
        {
            var products = await _productRepository.GetAllActiveWithStatsAsync(ct);
            var result = new ProductInsightsResultDto();
            var classifiable = products.Where(p => p.ViewCount >= MinViewsToClassify).ToList();
            if (classifiable.Count == 0)
            {
                return result;
            }

            // Ngưỡng "cao/thấp" tính theo trung bình của chính tập sản phẩm đang có, tự thích ứng theo quy mô shop
            var argView = classifiable.Average(p => p.ViewCount);
            var argSold = classifiable.Average(p => p.SoldCount);

            foreach (var p in classifiable)
            {
                var dto = new ProductInsightItemDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Slug = p.Slug.Value,
                    ThumbnailUrl = p.ResolveThumbnailUrl(),
                    ViewCount = p.ViewCount,
                    SoldCount = p.SoldCount
                };
                var highView = p.ViewCount >= argView;
                var highSold = p.SoldCount >= argSold;

                if (highView && !highSold)
                {
                    result.NeedsReview.Add(dto);
                }
                else if (highView && highSold)
                {
                    result.BestSellers.Add(dto);
                }
                else if (!highView && highSold)
                {
                    result.RepeatBuys.Add(dto);
                }
                else
                {
                    result.SlowMovers.Add(dto);
                }
            }
            result.NeedsReview = result.NeedsReview.OrderByDescending(x => x.ViewCount).ToList();
            result.BestSellers = result.BestSellers.OrderByDescending(x => x.SoldCount).ToList();
            result.RepeatBuys = result.RepeatBuys.OrderByDescending(x => x.SoldCount).ToList();
            result.SlowMovers = result.SlowMovers.OrderBy(x => x.ViewCount).ToList();
            return result;
        }
    }
}

