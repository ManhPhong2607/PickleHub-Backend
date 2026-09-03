using MediatR;
using PickleHub.AuditLog.Application.Features.DTOs;
using PickleHub.AuditLog.Domain.Repositories;
using PickleHub.Common.DTOs;

namespace PickleHub.AuditLog.Application.Features.GetAuditLogs
{
    public record GetAuditLogsQuery(
        Guid? ActorId,
        string? ActorRole,
        string? Action,
        string? EntityType,
        Guid? EntityId,
        DateTime? FromDate,
        DateTime? ToDate,
        int Page = 1,
        int PageSize = 20) : IRequest<PagedResult<AuditLogDto>>;

    public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public GetAuditLogsHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
        {
            var (items, totalItems) = await _auditLogRepository.GetPagedAsync(
                request.ActorId,
                request.ActorRole,
                request.Action,
                request.EntityType,
                request.EntityId,
                request.FromDate,
                request.ToDate,
                request.Page,
                request.PageSize,
                ct);

            return new PagedResult<AuditLogDto>
            {
                Items = items.Select(i => new AuditLogDto
                {
                    Id = i.Id,
                    ActorId = i.ActorId,
                    ActorRole = i.ActorRole,
                    ActorEmail = i.ActorEmail,
                    Action = i.Action,
                    EntityType = i.EntityType,
                    EntityId = i.EntityId,
                    Description = i.Description,
                    Metadata = i.Metadata,
                    OccurredAt = i.OccurredAt,
                    CreatedAt = i.CreatedAt
                }).ToList(),
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
