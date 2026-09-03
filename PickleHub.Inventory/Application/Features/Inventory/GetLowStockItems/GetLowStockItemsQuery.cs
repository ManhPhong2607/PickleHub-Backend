using MediatR;
using PickleHub.Inventory.Application.Features.DTOs;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.GetLowStockItems
{
    public record GetLowStockItemsQuery : IRequest<List<LowStockItemDto>>;

    public class GetLowStockItemsHandler : IRequestHandler<GetLowStockItemsQuery, List<LowStockItemDto>>
    {
        private readonly IInventoryItemRepository _inventoryRepository;

        public GetLowStockItemsHandler(IInventoryItemRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<List<LowStockItemDto>> Handle(GetLowStockItemsQuery request, CancellationToken ct)
        {
            //query db, mỗi item dùng LowStock riêng
            var items = await _inventoryRepository.GetLowStockItemsAsync(ct);

            return items.Select(i => new LowStockItemDto
            {
                ProductVariantId = i.ProductVariantId,
                ProductId = i.ProductId,
                SkuSnapshot = i.SkuSnapshot,
                Quantity = i.Quantity,
                ReservedQuantity = i.ReservedQuantity,
                AvailableQuantity = i.AvailableQuantity,
                LowStockThreshold = i.LowStockThreshold
            }).ToList();
        }
    }
}
