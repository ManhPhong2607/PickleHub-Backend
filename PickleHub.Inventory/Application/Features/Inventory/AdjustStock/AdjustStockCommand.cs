using MassTransit;
using MediatR;
using PickleHub.Common.Events.Inventory;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Inventory.Application.Features.DTOs;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.AdjustStock
{
    public record AdjustStockItemDto(
        Guid ProductVariantId,
        int Delta,
        string Reason,
        string? ReferenceId = null
    );

    public record AdjustStockCommand(
        Guid VariantId,
        int Delta,
        string Reason,
        string? ReferenceId = null
    ) : IRequest<InventoryItemDto>;

    public record BulkAdjustStockCommand(
        List<AdjustStockItemDto> Adjustments
    ) : IRequest<BulkAdjustResultDto>;

    public record BulkAdjustResultDto(
        bool Success,
        int Count,
        List<InventoryItemDto> Items
    );

    public class AdjustStockHandler : IRequestHandler<AdjustStockCommand, InventoryItemDto>
    {
        private readonly IInventoryItemRepository _inventoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICurrentUserService _currentUser;

        public AdjustStockHandler(
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

        public async Task<InventoryItemDto> Handle(AdjustStockCommand request, CancellationToken ct)
        {
            var item = await _inventoryRepository.GetByVariantIdAsync(request.VariantId, ct);
            if (item == null)
            {
                if (request.Delta > 0)
                {
                    item = InventoryItem.Create(
                        request.VariantId,
                        Guid.Empty,
                        $"SKU-{request.VariantId.ToString()[..8].ToUpper()}",
                        lowStockThreshold: 5,
                        initialQuantity: 0);
                    _inventoryRepository.Add(item);
                }
                else
                {
                    throw new NotFoundException($"Không tìm thấy bản ghi kho cho biến thể {request.VariantId}.");
                }
            }

            Guid? refId = null;
            if (!string.IsNullOrWhiteSpace(request.ReferenceId) && Guid.TryParse(request.ReferenceId, out var g))
            {
                refId = g;
            }

            int prevPhysical = item.Quantity;
            item.Adjust(request.Delta, refId, request.Reason);
            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new InventoryAdjustedEvent
            {
                ProductVariantId = item.ProductVariantId,
                ProductId = item.ProductId,
                PreviousPhysical = prevPhysical,
                NewPhysical = item.Quantity,
                Change = request.Delta,
                Reason = request.Reason,
                AdjustedBy = _currentUser.Email ?? "Admin",
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

    public class BulkAdjustStockHandler : IRequestHandler<BulkAdjustStockCommand, BulkAdjustResultDto>
    {
        private readonly IInventoryItemRepository _inventoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICurrentUserService _currentUser;

        public BulkAdjustStockHandler(
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

        public async Task<BulkAdjustResultDto> Handle(BulkAdjustStockCommand request, CancellationToken ct)
        {
            var resultItems = new List<InventoryItemDto>();
            var eventsToPublish = new List<InventoryAdjustedEvent>();

            foreach (var adj in request.Adjustments)
            {
                var item = await _inventoryRepository.GetByVariantIdAsync(adj.ProductVariantId, ct);
                if (item == null)
                {
                    if (adj.Delta > 0)
                    {
                        item = InventoryItem.Create(
                            adj.ProductVariantId,
                            Guid.Empty,
                            $"SKU-{adj.ProductVariantId.ToString()[..8].ToUpper()}",
                            lowStockThreshold: 5,
                            initialQuantity: 0);
                        _inventoryRepository.Add(item);
                    }
                    else
                    {
                        throw new NotFoundException($"Không tìm thấy bản ghi kho cho biến thể {adj.ProductVariantId}.");
                    }
                }

                Guid? refId = null;
                if (!string.IsNullOrWhiteSpace(adj.ReferenceId) && Guid.TryParse(adj.ReferenceId, out var g))
                {
                    refId = g;
                }

                int prevPhysical = item.Quantity;
                item.Adjust(adj.Delta, refId, adj.Reason);

                eventsToPublish.Add(new InventoryAdjustedEvent
                {
                    ProductVariantId = item.ProductVariantId,
                    ProductId = item.ProductId,
                    PreviousPhysical = prevPhysical,
                    NewPhysical = item.Quantity,
                    Change = adj.Delta,
                    Reason = adj.Reason,
                    AdjustedBy = _currentUser.Email ?? "Admin",
                    OccurredAt = DateTime.UtcNow
                });

                resultItems.Add(new InventoryItemDto
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
                });
            }

            await _unitOfWork.SaveChangesAsync(ct);

            foreach (var ev in eventsToPublish)
            {
                await _publishEndpoint.Publish(ev, ct);
            }

            return new BulkAdjustResultDto(true, resultItems.Count, resultItems);
        }
    }
}
