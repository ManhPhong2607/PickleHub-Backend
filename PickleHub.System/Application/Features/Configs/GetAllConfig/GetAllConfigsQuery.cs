using MediatR;
using PickleHub.System.Application.Features.DTOs;
using PickleHub.System.Domain.Repositories;

namespace PickleHub.System.Application.Features.Configs.GetAllConfig
{
    public record GetAllConfigsQuery : IRequest<List<SystemConfigDto>>;

    public class GetAllConfigsQueryHandler : IRequestHandler<GetAllConfigsQuery, List<SystemConfigDto>>
    {
        private readonly ISystemConfigRepository _configRepository;
        public GetAllConfigsQueryHandler(ISystemConfigRepository configRepository)
        {
            _configRepository = configRepository;
        }

        public async Task<List<SystemConfigDto>> Handle(GetAllConfigsQuery request, CancellationToken ct)
        {
            var configs = await _configRepository.GetAllAsync(ct);

            return configs.Select(c => new SystemConfigDto
            {
                Id = c.Id,
                Key = c.Key,
                Value = c.Value,
                Description = c.Description,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }
    }

}
