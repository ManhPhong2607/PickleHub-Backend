using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PickleHub.Catalog.Application.Features.Promotions.DTOs;
using PickleHub.Catalog.Domain.Repositories;
using PickleHub.Common.DTOs;

namespace PickleHub.Catalog.Application.Features.Promotions.GetPromotions
{
    public record GetPromotionsQuery(PickleHub.Catalog.Domain.Enums.PromotionStatus? Status = null, int Page = 1, int PageSize = 20) : IRequest<PagedResult<PromotionSummaryDto>>;

    public class GetPromotionsHandler : IRequestHandler<GetPromotionsQuery, PagedResult<PromotionSummaryDto>>
    {
        private readonly IPromotionRepository _promotionRepository;

        public GetPromotionsHandler(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public async Task<PagedResult<PromotionSummaryDto>> Handle(GetPromotionsQuery request, CancellationToken ct)
        {
            var (items, totalItems) = await _promotionRepository.GetPagedAsync(request.Status, request.Page, request.PageSize, ct);
            var dtos = items.Select(p => new PromotionSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                StartsAt = p.StartsAt,
                EndsAt = p.EndsAt,
                IsActive = p.IsActive,
                Priority = p.Priority,
                IsCurrentlyRunning = p.IsCurrentlyRunning,
                ProductCount = p.Items.Count
            }).ToList();

            return new PagedResult<PromotionSummaryDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
