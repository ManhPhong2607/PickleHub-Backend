using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.Events.Catalog;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class ProductUpdatedConsumer : IConsumer<ProductUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductUpdatedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
        {
            var message = context.Message;

            var log = AuditLogs.Create(
                action: "Product.Updated",
                entityType: "Product",
                entityId: message.ProductId,
                description: $"Sản phẩm '{message.ProductName}' được cập nhật",
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
