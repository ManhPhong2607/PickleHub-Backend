using MassTransit;
using MediatR;
using PickleHub.Common.Events.Inventory;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Inventory.Application.Features.DTOs;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.UpdateThreshold
{
    public record UpdateThresholdCommand(
        Guid VariantId, int Threshold) : IRequest<InventoryItemDto>;

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
            ICurrentUserService currentUser )
        {
            _inventoryRepository = inventoryRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _currentUser = currentUser;
        }

        public async Task<InventoryItemDto> Handle(UpdateThresholdCommand request, CancellationToken ct)
        {
            var item = await _inventoryRepository.GetByVariantIdAsync(request.VariantId, ct)
                ?? throw new NotFoundException("Không tìm thấy thông tin tồn kho.");

            var oldThreshold = item.LowStockThreshold;

            item.UpdateThreshold(request.Threshold);
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
                LowStockThreshold = item.LowStockThreshold,
                IsLowStock = item.IsLowStock,
                IsOutOfStock = item.IsOutOfStock,
                UpdatedAt = item.UpdatedAt ?? item.CreatedAt
            };
        }
    }

}
