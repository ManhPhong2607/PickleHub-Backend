using MediatR;
using PickleHub.Inventory.Application.Features.DTOs;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.CheckStock
{
    public record CheckStockQuery(
        Guid VariantId,
        int RequiredQuantity) : IRequest<CheckStockDto>;
    public class CheckStockHandler : IRequestHandler<CheckStockQuery, CheckStockDto>
    {
        private readonly IInventoryItemRepository _inventoryRepository;

        public CheckStockHandler(IInventoryItemRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<CheckStockDto> Handle(CheckStockQuery request, CancellationToken ct)
        {
            var item = await _inventoryRepository.GetByVariantIdAsync(request.VariantId, ct);

            if (item == null)
                return new CheckStockDto( request.VariantId, false, 0, request.RequiredQuantity);

            //check theo availablequantity
            return new CheckStockDto(
                request.VariantId,
                item.AvailableQuantity >= request.RequiredQuantity,
                item.AvailableQuantity,
                request.RequiredQuantity);
        }
    }
}
