using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.Inventory.Application.Features.DTOs;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.GetInventoryItem
{
    public record GetInventoryItemQuery(Guid VariantId) : IRequest<InventoryItemDto>;

    public class GetInventoryItemHandler
    : IRequestHandler<GetInventoryItemQuery, InventoryItemDto>
    {
        private readonly IInventoryItemRepository _inventoryRepository;

        public GetInventoryItemHandler(IInventoryItemRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<InventoryItemDto> Handle(
            GetInventoryItemQuery request, CancellationToken ct)
        {
            var item = await _inventoryRepository.GetByVariantIdAsync(request.VariantId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin tồn kho.");

            return new InventoryItemDto
            {
                Id = item.Id,
                ProductVariantId = item.ProductVariantId,
                ProductId = item.ProductId,
                SkuSnapshot = item.SkuSnapshot,
                Quantity = item.Quantity,
                ReservedQuantity = item.ReservedQuantity,
                AvailableQuantity = item.AvailableQuantity,
                LowStockThreshold = item.LowStockThreshold,
                IsLowStock = item.IsLowStock,
                IsOutOfStock = item.IsOutOfStock,
                UpdatedAt = item.UpdatedAt ?? item.CreatedAt
            };
        }
    }
}
