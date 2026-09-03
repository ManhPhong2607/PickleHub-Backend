using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.Events.Catalog;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class ProductStatusChangedConsumer : IConsumer<ProductStatusChangedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductStatusChangedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<ProductStatusChangedEvent> context)
        {
            var message = context.Message;

            var actionText = message.Action switch
            {
                "Published" => "được đăng bán",
                "Hidden" => "bị ẩn khỏi cửa hàng",
                "Restored" => "được khôi phục",
                _ => message.Action
            };

            var log = AuditLogs.Create(
                action: $"Product.{message.Action}",
                entityType: "Product",
                entityId: message.ProductId,
                description: $"Sản phẩm '{message.ProductName}' {actionText}",
                occurredAt: message.OccurredAt,
                actorId: message.ActorUserId,
                actorRole: "Admin",
                actorEmail: message.ActorEmail,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
