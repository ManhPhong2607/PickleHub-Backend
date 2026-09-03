using MediatR;
using MassTransit;
using PickleHub.Common.Events.System;
using PickleHub.Common.Interfaces;
using PickleHub.System.Application.Features.DTOs;
using PickleHub.System.Domain.Entities;
using PickleHub.System.Domain.Repositories;

namespace PickleHub.System.Application.Features.Configs.UpsertConfig
{
    //Dùng pattern Upsert — nếu key đã tồn tại thì update, chưa có thì create.
    public record UpsertConfigCommand (
        string Key, 
        string Value,
        string? Description) : IRequest<SystemConfigDto>;

    public class UpsertConfigCommandHandler : IRequestHandler<UpsertConfigCommand, SystemConfigDto>
    {
        private readonly ISystemConfigRepository _configRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICurrentUserService _currentUser;

        public UpsertConfigCommandHandler(ISystemConfigRepository configRepository, IUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint,
            ICurrentUserService currentUser)
        {
            _configRepository = configRepository;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _currentUser = currentUser;
        }

        public async Task<SystemConfigDto> Handle(UpsertConfigCommand request, CancellationToken ct)
        {
            var existing = await _configRepository.GetByKeyAsync(request.Key, ct);
            var oldValue = existing?.Value ?? string.Empty;
            if (existing != null)
            {
                existing.Update(request.Value, request.Description);
            }
            else
            {
                existing = SystemConfig.Create(request.Key, request.Value, request.Description);
                _configRepository.Add(existing);
            }
            await _unitOfWork.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new SystemConfigUpdatedEvent
            {
                Key = existing.Key,
                OldValue = oldValue,
                NewValue = existing.Value,
                UpdatedByUserId = _currentUser.UserId,
                UpdatedByEmail = _currentUser.Email ?? string.Empty,
                OccurredAt = DateTime.UtcNow
            }, ct);

            return new SystemConfigDto
            {
                Id = existing.Id,
                Key = existing.Key,
                Value = existing.Value,
                Description = existing.Description,
                UpdatedAt = existing.UpdatedAt
            };
        }
    }
}
