using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.Common.Events.Order;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OrderCreatedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var message = context.Message;

            var log = AuditLogs.Create(
                action: "Order.Created",
                entityType: "Order",
                entityId: message.OrderId,
                description: $"Đơn hàng được tạo bởi {message.CustomerEmail} " +
                             $"| Tổng tiền: {message.TotalAmount:N0}đ " +
                             $"| {message.Items.Count} sản phẩm",
                occurredAt: message.CreatedAt,
                actorId: message.CustomerId,
                actorRole: "Customer",
                actorEmail: message.CustomerEmail,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
