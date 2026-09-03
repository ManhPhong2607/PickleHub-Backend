using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.Common.Events.Order;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class OrderStatusUpdatedConsumer : IConsumer<OrderStatusUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OrderStatusUpdatedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<OrderStatusUpdatedEvent> context)
        {
            var message = context.Message;

            var description = $"Trạng thái đơn hàng thay đổi: " +
                              $"{message.OldStatus} → {message.NewStatus}";

            if (!string.IsNullOrEmpty(message.TrackingNumber))
                description += $" | Mã vận đơn: {message.TrackingNumber}";

            var log = AuditLogs.Create(
                action: "Order.StatusUpdated",
                entityType: "Order",
                entityId: message.OrderId,
                description: description,
                occurredAt: message.UpdatedAt,
                actorId: null,
                actorRole: "Admin",
                actorEmail: "admin",
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
