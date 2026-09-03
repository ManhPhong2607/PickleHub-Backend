using PickleHub.Common.Interfaces;
using MassTransit;
using PickleHub.AuditLog.Domain.Entities;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.Events.Authen;
using System.Text.Json;

namespace PickleHub.AuditLog.Infrastructure.Consumers
{
    public class UserLoggedInConsumer : IConsumer<UserLoggedInEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UserLoggedInConsumer(
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
        {
            var message = context.Message;

            var log = AuditLogs.Create(
                action: "User.LoggedIn",
                entityType: "User",
                entityId: message.UserId,
                description: $"{message.Email} đăng nhập | Role: {message.Role}",
                occurredAt: message.OccurredAt,
                actorId: message.UserId,
                actorRole: message.Role,
                actorEmail: message.Email,
                metadata: JsonSerializer.Serialize(message));

            _auditLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
