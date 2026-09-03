using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.Common.Events.Customers;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class CustomerBlockedConsumer : IConsumer<CustomerBlockedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CustomerBlockedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<CustomerBlockedEvent> context)
        {
            var message = context.Message;

            var action = message.IsBlocked ? "Customer.Blocked" : "Customer.Unblocked";
            var description = message.IsBlocked
                ? $"Tài khoản {message.CustomerEmail} bị khóa bởi Admin"
                : $"Tài khoản {message.CustomerEmail} được mở khóa bởi Admin";

            var log = AuditLogs.Create(
                action: action,
                entityType: "Customer",
                entityId: message.CustomerId,
                description: description,
                occurredAt: message.OccurredAt,
                actorId: null,
                actorRole: "Admin",
                actorEmail: "admin",
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
