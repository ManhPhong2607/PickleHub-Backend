using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.Common.Events.Authen;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UserRegisteredConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
        {
            var message = context.Message;

            var log = AuditLogs.Create(
                action: "User.Registered",
                entityType: "User",
                entityId: message.UserId,
                description: $"Tài khoản mới đăng ký: {message.Email}",
                occurredAt: message.RegisteredAt,
                actorId: message.UserId,
                actorRole: "Customer",
                actorEmail: message.Email,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
