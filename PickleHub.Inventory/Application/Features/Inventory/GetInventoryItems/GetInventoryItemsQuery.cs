using MediatR;
using PickleHub.Common.DTOs;
using PickleHub.Inventory.Application.Features.DTOs;
using PickleHub.Inventory.Domain.Repositories;
using PickleHub.Inventory.Domain.Entities;
namespace PickleHub.Inventory.Application.Features.Inventory.GetInventoryItems
{
    public record GetInventoryItemsQuery(
        Guid? ProductId,
        bool? IsLowStock,
        int Page = 1,
        int PageSize = 20) : IRequest<PagedResult<InventoryItemDto>>;

    public class GetInventoryItemsHandler : IRequestHandler<GetInventoryItemsQuery, PagedResult<InventoryItemDto>>
    {
        private readonly IInventoryItemRepository _inventoryRepository;

        public GetInventoryItemsHandler(IInventoryItemRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<PagedResult<InventoryItemDto>> Handle( GetInventoryItemsQuery request, CancellationToken ct)
        {
            var (items, totalItems) = await _inventoryRepository.GetPagedAsync(
                request.ProductId,
                request.IsLowStock,
                request.Page,
                request.PageSize,
                ct);

            return new PagedResult<InventoryItemDto>
            {
                Items = items.Select(i=> new InventoryItemDto
                {
                    Id = i.Id,
                    ProductVariantId = i.ProductVariantId,
                    ProductId = i.ProductId,
                    SkuSnapshot = i.SkuSnapshot,
                    Quantity = i.Quantity,
                    ReservedQuantity = i.ReservedQuantity,
                    AvailableQuantity = i.AvailableQuantity,
                    LowStockThreshold = i.LowStockThreshold,
                    IsLowStock = i.IsLowStock,
                    IsOutOfStock = i.IsOutOfStock,
                    UpdatedAt = i.UpdatedAt ?? i.CreatedAt
                }).ToList(),
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }      
    }
}
