using MassTransit;
using Microsoft.Extensions.Logging;
using PickleHub.Common.Events.Catalog;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using PickleHub.Inventory.Domain.Entities;
using PickleHub.Inventory.Domain.Repositories;

namespace PickleHub.Inventory.Infrastructure.Consumers
{
    public class ProductVariantCreatedConsumer : IConsumer<ProductVariantCreatedEvent>
    {
        private readonly IInventoryItemRepository _inventoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductVariantCreatedConsumer> _logger;

        public ProductVariantCreatedConsumer(
            IInventoryItemRepository inventoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<ProductVariantCreatedConsumer> logger)
        {
            _inventoryRepository = inventoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductVariantCreatedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation(
                "Đang xử lý sự kiện ProductVariantCreated: VariantId={VariantId}, ProductId={ProductId}, Sku={Sku}",
                message.VariantId, message.ProductId, message.Sku);

            // 1. Kiểm tra Idempotency - nếu đã tồn tại thì bỏ qua tránh trùng lặp
            var exists = await _inventoryRepository.ExistsByVariantIdAsync(message.VariantId, context.CancellationToken);
            if (exists)
            {
                _logger.LogInformation(
                    "Bản ghi kho cho VariantId {VariantId} (Sku: {Sku}) đã tồn tại. Bỏ qua để đảm bảo Idempotent.",
                    message.VariantId, message.Sku);
                return;
            }

            try
            {
                // 2. Tự động tạo bản ghi kho mới: tồn kho vật lý = 0, tồn tạm giữ = 0, ngưỡng cảnh báo = 5
                var inventoryItem = InventoryItem.Create(
                    productVariantId: message.VariantId,
                    productId: message.ProductId,
                    skuSnapshot: message.Sku,
                    lowStockThreshold: 5,
                    initialQuantity: 0
                );

                _inventoryRepository.Add(inventoryItem);
                await _unitOfWork.SaveChangesAsync(context.CancellationToken);

                _logger.LogInformation(
                    "Đã tự động tạo bản ghi kho cho VariantId {VariantId}, Sku {Sku} thành công (Quantity=0, ReservedQuantity=0).",
                    message.VariantId, message.Sku);
            }
            catch (DuplicateOperationException ex)
            {
                // Xử lý race condition khi 2 event giống nhau đến cùng lúc và vi phạm unique constraint
                _logger.LogWarning(
                    "Bản ghi kho cho VariantId {VariantId} đã được tạo bởi tiến trình song song khác: {Message}",
                    message.VariantId, ex.Message);
            }
        }
    }
}
