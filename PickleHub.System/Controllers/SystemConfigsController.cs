using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.System.Application.Features.Configs.GetAllConfig;
using PickleHub.System.Application.Features.Configs.GetConfig;
using PickleHub.System.Application.Features.Configs.UpsertConfig;

namespace PickleHub.System.Controllers
{
    [ApiController]
    [Route("system/configs")]
    public class SystemConfigsController(ISender mediator) : ControllerBase
    {
        // Public — service khác gọi sang đọc config
        [HttpGet("{key}")]
        public async Task<IActionResult> GetByKey(string key, CancellationToken ct)
        {
            var result = await mediator.Send(new GetConfigQuery(key), ct);
            return Ok(result);
        }

        // Admin only — xem toàn bộ config
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await mediator.Send(new GetAllConfigsQuery(), ct);
            return Ok(result);
        }

        // tạo hoặc cập nhật config
        [HttpPut("{key}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upsert(
            string key, [FromBody] UpsertConfigRequest command, CancellationToken ct)
        {
            var result = await mediator.Send(
                new UpsertConfigCommand(key, command.Value, command.Description), ct);
            return Ok(result);
        }

        public record UpsertConfigRequest(string Value, string? Description);
    }

}
