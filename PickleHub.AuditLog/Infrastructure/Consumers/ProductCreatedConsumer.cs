using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.Events.Catalog;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductCreatedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            var message = context.Message;

            var log = AuditLogs.Create(
                action: "Product.Created",
                entityType: "Product",
                entityId: message.ProductId,
                description: $"Sản phẩm '{message.ProductName}' được tạo" +
                             $" | Danh mục: {message.CategoryName}" +
                             $" | Thương hiệu: {message.BrandName}",
                occurredAt: message.OccurredAt,
                actorId: message.CreatedByUserId,
                actorRole: "Admin",
                actorEmail: message.CreatedByEmail,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
