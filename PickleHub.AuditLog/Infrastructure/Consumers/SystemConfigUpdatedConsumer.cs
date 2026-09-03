using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.Events.System;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class SystemConfigUpdatedConsumer : IConsumer<SystemConfigUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SystemConfigUpdatedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<SystemConfigUpdatedEvent> context)
        {
            var message = context.Message;

            var log = AuditLogs.Create(
                action: "SystemConfig.Updated",
                entityType: "SystemConfig",
                entityId: null,
                description: $"Cấu hình '{message.Key}' thay đổi:" +
                             $" '{message.OldValue}' → '{message.NewValue}'",
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
