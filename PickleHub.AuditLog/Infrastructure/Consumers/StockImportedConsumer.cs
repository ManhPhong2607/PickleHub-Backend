using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.Events.Inventory;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class StockImportedConsumer : IConsumer<StockImportedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StockImportedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<StockImportedEvent> context)
        {
            var message = context.Message;

            var log = AuditLogs.Create(
                action: "Stock.Imported",
                entityType: "InventoryItem",
                entityId: message.ProductVariantId,
                description: $"Nhập kho {message.QuantityImported} sản phẩm" +
                             $" | SKU: {message.SkuSnapshot}" +
                             $" | Tồn kho sau: {message.QuantityAfter}" +
                             $"{(string.IsNullOrEmpty(message.Note) ? "" : $" | Ghi chú: {message.Note}")}",
                occurredAt: message.OccurredAt,
                actorId: message.ImportedByUserId,
                actorRole: "Admin",
                actorEmail: message.ImportedByEmail,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
