using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Customers.Application.Features.LoyaltyTiers.CreateLoyaltyTier;
using PickleHub.Customers.Application.Features.LoyaltyTiers.DeleteLoyaltyTier;
using PickleHub.Customers.Application.Features.LoyaltyTiers.GetLoyaltyTiers;
using PickleHub.Customers.Application.Features.LoyaltyTiers.UpdateLoyaltyTier;

namespace PickleHub.Customers.Controllers
{
    [ApiController]
    [Route("loyalty-tiers")]
    public class LoyaltyTiersController(ISender mediator) : ControllerBase
    {
        // Public - FE cần đọc danh sách hạng để hiển thị (VD: trang giới thiệu chương trình thành viên) mà không nhất thiết phải đăng nhập.
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await mediator.Send(new GetLoyaltyTiersQuery(), ct);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateLoyaltyTierCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetAll), result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLoyaltyTierCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command with { Id = id }, ct);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await mediator.Send(new DeleteLoyaltyTierCommand(id), ct);
            return NoContent();
        }
    }
}
