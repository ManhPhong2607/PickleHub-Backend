using MassTransit;
using MediatR;
using PickleHub.Common.Events.Inventory;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Inventory.Application.Features.DTOs;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.UpdateThreshold
{
    public record UpdateThresholdCommand(
        Guid VariantId,
        int Threshold,
        Guid? ProductId = null,
        string? SkuSnapshot = null,
        int? CurrentQuantity = null) : IRequest<InventoryItemDto>;

    public class UpdateThresholdHandler : IRequestHandler<UpdateThresholdCommand, InventoryItemDto>
    {
        private readonly IInventoryItemRepository _inventoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICurrentUserService _currentUser;

        public UpdateThresholdHandler(
            IInventoryItemRepository inventoryRepository,
            IUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint,
            ICurrentUserService currentUser)
        {
            _inventoryRepository = inventoryRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _currentUser = currentUser;
        }

        public async Task<InventoryItemDto> Handle(UpdateThresholdCommand request, CancellationToken ct)
        {
            var item = await _inventoryRepository.GetByVariantIdAsync(request.VariantId, ct);
            if (item == null)
            {
                item = await _inventoryRepository.GetByIdAsync(request.VariantId, ct);
            }

            var oldThreshold = item?.LowStockThreshold ?? 0;

            if (item == null)
            {
                var sku = !string.IsNullOrWhiteSpace(request.SkuSnapshot)
                    ? request.SkuSnapshot
                    : $"SKU-{request.VariantId.ToString()[..8].ToUpper()}";
                var prodId = request.ProductId ?? Guid.Empty;
                var initialQty = request.CurrentQuantity.HasValue && request.CurrentQuantity.Value > 0
                    ? request.CurrentQuantity.Value
                    : 15;

                item = InventoryItem.Create(
                    request.VariantId,
                    prodId,
                    sku,
                    request.Threshold,
                    initialQty);

                _inventoryRepository.Add(item);
            }
            else
            {
                item.UpdateThreshold(request.Threshold);

                // Tự động khôi phục số lượng tồn kho nếu trước đó bị về 0 do thiếu bản ghi
                if (item.Quantity == 0 && request.CurrentQuantity.HasValue && request.CurrentQuantity.Value > 0)
                {
                    item.Import(request.CurrentQuantity.Value, note: "Khôi phục số lượng tồn kho ban đầu");
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new StockThresholdUpdatedEvent
            {
                ProductVariantId = item.ProductVariantId,
                SkuSnapshot = item.SkuSnapshot,
                OldThreshold = oldThreshold,
                NewThreshold = request.Threshold,
                UpdatedByUserId = _currentUser.UserId,
                UpdatedByEmail = _currentUser.Email ?? string.Empty,
                OccurredAt = DateTime.UtcNow
            }, ct);

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
