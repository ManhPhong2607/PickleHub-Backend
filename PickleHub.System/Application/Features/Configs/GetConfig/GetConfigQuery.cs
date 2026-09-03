using MediatR;
using PickleHub.Common.Exceptions;
using PickleHub.System.Application.Features.DTOs;
using PickleHub.System.Domain.Entities;
using PickleHub.System.Domain.Repositories;

namespace PickleHub.System.Application.Features.Configs.GetConfig
{
    public record GetConfigQuery(string Key) : IRequest<SystemConfigDto>;
 
    public class GetConfigQueryHandler : IRequestHandler<GetConfigQuery, SystemConfigDto>
    {
        private readonly ISystemConfigRepository _configRepository;
        public GetConfigQueryHandler(ISystemConfigRepository configRepository)
        {
            _configRepository = configRepository;
        }

        public async Task<SystemConfigDto> Handle(GetConfigQuery request, CancellationToken ct)
        {
            var config = await _configRepository.GetByKeyAsync(request.Key, ct)
             ?? throw new NotFoundException($"Không tìm thấy config với key '{request.Key}'.");

            return new SystemConfigDto
            {
                Id = config.Id,
                Key = config.Key,
                Value = config.Value,
                Description = config.Description,
                UpdatedAt = config.UpdatedAt
            };
        }
    }
}
