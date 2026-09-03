using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PickleHub.Common.Events.Inventory;
using PickleHub.Common.Interfaces;
using PickleHub.Inventory.Application.Features.DTOs;
using PickleHub.Inventory.Application.Settings;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Application.Features.Inventory.ImportStock
{
    public record ImportStockCommand(
        Guid ProductVariantId,
        Guid ProductId,
        string SkuSnapshot,
        int Quantity,
        string? Note = null) : IRequest<InventoryItemDto>;

    public class ImportStockHandler : IRequestHandler<ImportStockCommand, InventoryItemDto>
    {
        private const int MaxRetries = 3;

        private readonly IInventoryItemRepository _inventoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly InventorySettings _settings;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICurrentUserService _currentUser;
        public ImportStockHandler(
            IInventoryItemRepository inventoryRepository,
            IUnitOfWork unitOfWork,
            IOptions<InventorySettings> settings,
            IPublishEndpoint publishEndpoint, 
            ICurrentUserService currentUser)
        {
            _inventoryRepository = inventoryRepository;
            _unitOfWork = unitOfWork;
            _settings = settings.Value;
            _publishEndpoint = publishEndpoint;
            _currentUser = currentUser;
        }

        public async Task<InventoryItemDto> Handle(ImportStockCommand request, CancellationToken ct)
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    var item = await _inventoryRepository.GetByVariantIdAsync(request.ProductVariantId, ct);

                    if (item == null)
                    {
                        var threshold = _settings.DefaultLowStockThreshold;
                        item = InventoryItem.Create(
                            request.ProductVariantId,
                            request.ProductId,
                            request.SkuSnapshot,
                            threshold);

                        _inventoryRepository.Add(item);
                    }

                    item.Import(request.Quantity, note: request.Note);
                    await _unitOfWork.SaveChangesAsync(ct);

                    await _publishEndpoint.Publish(new StockImportedEvent
                    {
                        InventoryItemId = item.Id,
                        ProductVariantId = item.ProductVariantId,
                        SkuSnapshot = item.SkuSnapshot,
                        QuantityImported = request.Quantity,
                        QuantityAfter = item.Quantity,
                        Note = request.Note,
                        ImportedByUserId = _currentUser.UserId,
                        ImportedByEmail = _currentUser.Email ?? string.Empty,
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
                catch (DbUpdateConcurrencyException ex)
                {
                    if (attempt == MaxRetries - 1)
                        throw;

                    // Reload entity entries from DB to get the latest Version
                    foreach (var entry in ex.Entries)
                    {
                        await entry.ReloadAsync(ct);
                    }
                }
            }

            // Unreachable, but required by compiler
            throw new InvalidOperationException("Import failed after maximum retries.");
        }
    }
}
