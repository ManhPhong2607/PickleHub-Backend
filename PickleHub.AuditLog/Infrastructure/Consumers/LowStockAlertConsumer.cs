using MassTransit;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.Events.Inventory;
using PickleHub.Common.Interfaces;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class LowStockAlertConsumer : IConsumer<LowStockAlertEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LowStockAlertConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<LowStockAlertEvent> context)
        {
            var message = context.Message;

            // Event do hệ thống tự sinh (không phải người dùng) nên actorId = Guid.Empty
            var log = AuditLogs.Create(
                action: "Stock.LowStockAlert",
                entityType: "InventoryItem",
                entityId: message.ProductVariantId,
                description: $"Cảnh báo sắp hết hàng - SKU: {message.SkuSnapshot}" +
                             $" | Tồn kho khả dụng: {message.AvailableQuantity}" +
                             $" | Ngưỡng cảnh báo: {message.LowStockThreshold}",
                occurredAt: message.OccurredAt,
                actorId: Guid.Empty,
                actorRole: "System",
                actorEmail: null,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
