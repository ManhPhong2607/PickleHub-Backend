using MediatR;
using PickleHub.Catalog.Application.Features.Products.DTOs;
using PickleHub.Catalog.Application.Mappings;
using PickleHub.Catalog.Domain.Repositories;

namespace PickleHub.Catalog.Application.Features.Products.GetProducts
{
    public record GetTrendingProductsQuery(int Days = 7, int Limit = 10) : IRequest<List<TrendingProductDto>>;

    public class GetTrendingProductsHandler : IRequestHandler<GetTrendingProductsQuery, List<TrendingProductDto>>
    {
        private readonly IProductRepository _productRepository;
        public GetTrendingProductsHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public async Task<List<TrendingProductDto>> Handle(GetTrendingProductsQuery request, CancellationToken ct)
        {
            //xác định khoảng thời gian hiện tại và trước đó 
            var days = request.Days <= 0 ? 7 : request.Days;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentStart = today.AddDays(-(days - 1));
            var previousStart = currentStart.AddDays(-days);
            var previousEnd = currentStart.AddDays(-1);

            // lấy lượt xem theo ngày tron cả 2 kỳ và cộng tổng view của từng sản phẩm trong 2 kỳ
            var rows = await _productRepository.GetViewDailyInRangeAsync(previousStart, today, ct);
            var currentViews = rows.Where(r => r.ViewDate >= currentStart && r.ViewDate <= today)
                .GroupBy(r => r.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.ViewCount));

            var previousViews = rows.Where(r => r.ViewDate >= previousStart && r.ViewDate <= previousEnd)
                .GroupBy(r => r.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.ViewCount));

            // lấy tất cả productId xuất hiện trong cả 2 kỳ 
            var productIds = currentViews.Keys.Union(previousViews.Keys).ToList();
            if (productIds.Count == 0)
            {
                return new List<TrendingProductDto>();
            }

            var products = await _productRepository.GetByIdsAsync(productIds, ct);

            // tính tăng trưởng và điểm trending cho từng sản phẩm
            var scored = products.Select(p =>
            {
                var current = currentViews.GetValueOrDefault(p.Id, 0);
                var previous = previousViews.GetValueOrDefault(p.Id, 0);
                var isNew = previous == 0 && current > 0;
                var dto = new TrendingProductDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Slug = p.Slug.Value,
                    ThumbnailUrl = p.ResolveThumbnailUrl(),
                    CurrentPeriodViews = current,
                    PreviousPeriodViews = previous,
                    GrowthPercent = previous == 0 ? null : Math.Round((decimal)(current - previous) / previous * 100, 1),
                    IsNewLyTrending = isNew
                };
                // Điểm dùng để sort - sản phẩm mới nổi (không có kỳ trước để so sánh) được
                // ưu tiên theo view tuyệt đối, cộng thêm hằng số lớn để luôn đứng trên nhóm có %
                // tăng trưởng bình thường (vì "mới nổi" đáng chú ý hơn "tăng trưởng %" thông thường).
                var sortScore = isNew ? 100_000m + current : (dto.GrowthPercent ?? 0m);
                return (Dto: dto, SortScore: sortScore);
            
            })
                .Where(x => x.Dto.CurrentPeriodViews > 0) //đã tụt về 0 view kỳ này thì không còn "đang trending"
                .OrderByDescending(x => x.SortScore)
                .Take(request.Limit)
                .Select(x => x.Dto)
                .ToList();
            return scored;
        }
    }
}
