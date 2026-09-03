using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.Events.Inventory;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class StockThresholdUpdatedConsumer : IConsumer<StockThresholdUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StockThresholdUpdatedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<StockThresholdUpdatedEvent> context)
        {
            var message = context.Message;

            var log = AuditLogs.Create(
                action: "Stock.ThresholdUpdated",
                entityType: "InventoryItem",
                entityId: message.ProductVariantId,
                description: $"Ngưỡng cảnh báo SKU: {message.SkuSnapshot}" +
                             $" thay đổi: {message.OldThreshold} → {message.NewThreshold}",
                occurredAt: message.OccurredAt,
                actorId: message.UpdatedByUserId,
                actorRole: "Admin",
                actorEmail: message.UpdatedByEmail,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
