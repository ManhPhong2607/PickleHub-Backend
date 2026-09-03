using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.AuditLog.Application.Features.GetAuditLogs;

namespace PickleHub.AuditLog.Controllers
{
    [ApiController]
    [Route("audit-logs")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] GetAuditLogsQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }
    }
}
