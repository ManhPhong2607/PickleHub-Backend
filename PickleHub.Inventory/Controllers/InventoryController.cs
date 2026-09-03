using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Inventory.Application.Features.Inventory.CheckStock;
using PickleHub.Inventory.Application.Features.Inventory.DeleteInventoryItem;
using PickleHub.Inventory.Application.Features.Inventory.ExportInventoryExcel;
using PickleHub.Inventory.Application.Features.Inventory.GetInventoryItem;
using PickleHub.Inventory.Application.Features.Inventory.GetInventoryItems;
using PickleHub.Inventory.Application.Features.Inventory.GetLowStockItems;
using PickleHub.Inventory.Application.Features.Inventory.ImportStock;
using PickleHub.Inventory.Application.Features.Inventory.ReleaseStock;
using PickleHub.Inventory.Application.Features.Inventory.ReserveStock;
using PickleHub.Inventory.Application.Features.Inventory.UpdateThreshold;

namespace PickleHub.Inventory.Controllers
{
    [ApiController]
    [Route("inventory")]
    [Authorize(Roles = "Admin")]
    public class InventoryController(ISender mediator, IConfiguration config) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetInventoryItemsQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("variants/{variantId:guid}")]
        public async Task<IActionResult> GetByVariantId(Guid variantId, CancellationToken ct)
        {
            var result = await mediator.Send(new GetInventoryItemQuery(variantId), ct);
            return Ok(result);
        }

        // Public — Order Service gọi trước checkout
        [HttpGet("variants/{variantId:guid}/check")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckStock(
            Guid variantId,
            [FromQuery] int requiredQuantity,
            CancellationToken ct)
        {
            var result = await mediator.Send(new CheckStockQuery(variantId, requiredQuantity), ct);
            return Ok(result);
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock(CancellationToken ct)
        {
            var result = await mediator.Send(new GetLowStockItemsQuery(), ct);
            return Ok(result);
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(
            [FromBody] ImportStockCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPatch("variants/{variantId:guid}/threshold")]
        public async Task<IActionResult> UpdateThreshold(
            Guid variantId,
            [FromBody] UpdateThresholdRequest body,
            CancellationToken ct)
        {
            var result = await mediator.Send(new UpdateThresholdCommand(variantId, body.Threshold, body.ProductId, body.SkuSnapshot, body.CurrentQuantity), ct);
            return Ok(result);
        }

        [HttpDelete("variants/{variantId:guid}")]
        public async Task<IActionResult> Delete(Guid variantId, CancellationToken ct)
        {
            await mediator.Send(new DeleteInventoryItemCommand(variantId), ct);
            return NoContent();
        }

        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(IFormFile file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return BadRequest("Vui lòng chọn file Excel.");

            using var stream = file.OpenReadStream();
            var result = await mediator.Send(new ImportStockFromExcelCommand(stream), ct);
            return Ok(result);
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(CancellationToken ct)
        {
            var bytes = await mediator.Send(new ExportInventoryToExcelQuery(), ct);
            var fileName = $"Inventory_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("import-template-excel")]
        public async Task<IActionResult> ImportTemplateExcel(CancellationToken ct)
        {
            var bytes = await mediator.Send(new GetImportTemplateExcelQuery(), ct);
            var fileName = $"inventory_import_template_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost("reserve")]
        [AllowAnonymous]
        public async Task<IActionResult> Reserve([FromBody] ReserveStockRequest body, CancellationToken ct)
        {
            var internalToken = config["Security:InternalApiKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
            if (!Request.Headers.TryGetValue("X-Internal-Key", out var headerKey) || headerKey != internalToken)
            {
                return Unauthorized("Yêu cầu này không hợp lệ hoặc thiếu mã khóa dịch vụ nội bộ.");
            }

            var result = await mediator.Send(
                new ReserveStockCommand(body.VariantId, body.Quantity, body.OrderId), ct);
            if (!result.Success) return Conflict(result);

            return Ok(result);
        }

        [HttpPost("release")]
        [AllowAnonymous]
        public async Task<IActionResult> Release([FromBody] ReleaseStockRequest body, CancellationToken ct)
        {
            var internalToken = config["Security:InternalApiKey"]
                  ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
            if (!Request.Headers.TryGetValue("X-Internal-Key", out var headerKey) || headerKey != internalToken)
            {
                return Unauthorized("Yêu cầu này không hợp lệ hoặc thiếu mã khóa dịch vụ nội bộ.");
            }
            var result = await mediator.Send(new ReleaseStockCommand(
                body.OrderId,
                [new ReleaseStockItem(body.VariantId, body.Quantity)]), ct);
            return Ok(result);
        }

        public record UpdateThresholdRequest(int Threshold, Guid? ProductId = null, string? SkuSnapshot = null, int? CurrentQuantity = null);

        // thêm OrderId bắt buộc (không nullable) — đây là ReferenceId dùng để chống Reserve/Release trùng lặp khi CartOrder timeout & gọi lại (retry từ client).
        public record ReserveStockRequest(Guid VariantId, int Quantity, Guid OrderId);
        public record ReleaseStockRequest(Guid VariantId, int Quantity, Guid OrderId);
    }
}
