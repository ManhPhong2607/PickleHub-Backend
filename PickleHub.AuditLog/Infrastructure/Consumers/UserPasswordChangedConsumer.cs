using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.Events.Authen;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class UserPasswordChangedConsumer : IConsumer<UserPasswordChangedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UserPasswordChangedConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<UserPasswordChangedEvent> context)
        {
            var message = context.Message;

            var description = message.Action == "Reset"
                ? $"Mật khẩu của {message.Email} được đặt lại qua email"
                : $"{message.Email} tự đổi mật khẩu";

            var log = AuditLogs.Create(
                action: $"User.Password{message.Action}",
                entityType: "User",
                entityId: message.UserId,
                description: description,
                occurredAt: message.OccurredAt,
                actorId: message.UserId,
                actorRole: "Customer",
                actorEmail: message.Email,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
