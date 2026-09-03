using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.Common.Events.Order;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OrderCancelledConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
        {
            var message = context.Message;

            var log = AuditLogs.Create(
                action: "Order.Cancelled",
                entityType: "Order",
                entityId: message.OrderId,
                description: $"Đơn hàng bị hủy bởi {message.CancelledBy}" +
                             $"{(string.IsNullOrEmpty(message.CancelReason) ? "" : $" | Lý do: {message.CancelReason}")}",
                occurredAt: message.CancelledAt,
                actorId: message.CustomerId,
                actorRole: message.CancelledBy,
                actorEmail: message.CustomerEmail,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
