using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Customers.Application.Features.Customers.BlockCustomer;
using PickleHub.Customers.Application.Features.Customers.GetCustomerDetail;
using PickleHub.Customers.Application.Features.Customers.GetCustomerInternal;
using PickleHub.Customers.Application.Features.Customers.GetCustomers;
using PickleHub.Customers.Application.Features.Customers.GetDashboardSummary;
using PickleHub.Customers.Application.Features.Customers.GetLoyalty;
using PickleHub.Customers.Application.Features.Customers.GetMe;
using PickleHub.Customers.Application.Features.Customers.UpdateMe;

namespace PickleHub.Customers.Controllers
{
    [ApiController]
    [Route("customers")]
    public class CustomersController(ISender mediator, IConfiguration config) : ControllerBase
    {
        // Customer tự xem/sửa thông tin

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var result = await mediator.Send(new GetMeQuery(), ct);
            return Ok(result);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(
            [FromBody] UpdateMeCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpGet("me/loyalty")]
        [Authorize]
        public async Task<IActionResult> GetLoyalty(CancellationToken ct)
        {
            var result = await mediator.Send(new GetLoyaltyQuery(), ct);
            return Ok(result);
        }

        // Admin quản lý 

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] GetCustomersQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{customerId:guid}/detail")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDetail(Guid customerId, CancellationToken ct)
        {
            var result = await mediator.Send(new GetCustomerDetailQuery(customerId), ct);
            return Ok(result);
        }

        [HttpPatch("{customerId:guid}/block")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Block(
            Guid customerId, [FromBody] BlockCustomerRequest body, CancellationToken ct)
        {
            await mediator.Send(new BlockCustomerCommand(customerId, body.IsBlocked), ct);
            return NoContent();
        }

        [HttpGet("dashboard/summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DashboardSummary(CancellationToken ct)
        {
            var result = await mediator.Send(new GetDashboardSummaryQuery(), ct);
            return Ok(result);
        }

        // Internal — dùng cho service khác call sang
        [HttpGet("~/internal/customers/{userId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByUserIdInternal(Guid userId, CancellationToken ct)
        {
            var internalToken = config["Security:InternalApiKey"]
                 ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
            if (!Request.Headers.TryGetValue("X-Internal-Key", out var headerKey) || headerKey != internalToken)
            {
                return Unauthorized("Yêu cầu này không hợp lệ hoặc thiếu mã khóa dịch vụ nội bộ.");
            }

            var result = await mediator.Send(new GetCustomerInternalQuery(userId), ct);
            if (result == null) return NotFound();

            return Ok(result);
        }   
    }

    public record BlockCustomerRequest(bool IsBlocked);
}
