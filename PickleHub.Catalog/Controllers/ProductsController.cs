using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Catalog.Application.Features.Products.CreateProduct;
using PickleHub.Catalog.Application.Features.Products.DeleteProduct;
using PickleHub.Catalog.Application.Features.Products.GetProducts;
using PickleHub.Catalog.Application.Features.Products.PublishProduct;
using PickleHub.Catalog.Application.Features.Products.RestoreProduct;
using PickleHub.Catalog.Application.Features.Products.UpdateProduct;

namespace PickleHub.Catalog.Controllers
{
    [ApiController]
    [Route("products")]
    public class ProductsController(ISender mediator, IConfiguration config) : ControllerBase
    {
        //public
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] GetProductsQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("{value}")]
        public async Task<IActionResult> GetBySlugOrId(string value, CancellationToken ct)
        {
            if (Guid.TryParse(value, out var id))
                return Ok(await mediator.Send(new GetProductByIdQuery(id), ct));

            return Ok(await mediator.Send(new GetProductBySlugQuery(value), ct));
        }

        [HttpGet("{id:guid}/related")]
        public async Task<IActionResult> GetRelated(Guid id, [FromQuery] int limit = 8, CancellationToken ct = default)
        {
            var result = await mediator.Send(new GetRelatedProductsQuery(id, limit), ct);
            return Ok(result);
        }


        //admin
        [HttpGet("~/admin/products")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Search([FromQuery] GetAdminProductsQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetBySlugOrId), new { value = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command with { Id = id }, ct);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await mediator.Send(new DeleteProductCommand(id), ct);
            return NoContent();
        }

        [HttpPatch("{id:guid}/publish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
        {
            await mediator.Send(new PublishProductCommand(id), ct);
            return NoContent();
        }

        [HttpPatch("{id:guid}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            await mediator.Send(new RestoreProductCommand(id), ct);
            return NoContent();
        }

        [HttpGet("~/admin/products/trending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTrending([FromQuery] int days = 7,[FromQuery] int limit =10, CancellationToken ct = default)
        {
            var result = await mediator.Send(new GetTrendingProductsQuery(days, limit), ct);
            return Ok(result);
        }

        [HttpGet("~/admin/products/insights")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetInsights(CancellationToken ct)
        {
            var result = await mediator.Send(new GetProductInsightsQuery(), ct);
            return Ok(result);
        }

        [HttpGet("~/admin/products/{value}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetForAdmin(string value, CancellationToken ct)
        {
            var result = await mediator.Send(new GetAdminProductDetailQuery(value), ct);
            if (result == null) return NotFound();
            return Ok(result);
        }


        // Endpoint nội bộ cho service khác gọi sang (CartOrder,Payment...) khi chỉ cần kiểm tra/lấy dữ liệu sản phẩm, KHÔNG tính là khách xem hàng
        // nên KHÔNG tăng ViewCount (khác route public GET /products/{value} ở trên).
        [HttpGet("~/internal/products/{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductInternal(Guid id, CancellationToken ct)
        {
            var internalToken = config["Security:InternalApiKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
            if (!Request.Headers.TryGetValue("X-Internal-Key", out var headerKey) || headerKey != internalToken)
            {
                return Unauthorized("Yêu cầu này không hợp lệ hoặc thiếu mã khóa dịch vụ nội bộ.");
            }

            var result = await mediator.Send(new GetProductDetailInternalQuery(id), ct);
            if (result == null) return NotFound();

            return Ok(result);
        }

        // Endpoint nội bộ cho Inventory Service đồng bộ
        // danh sách variant (kể cả variant chưa từng nhập kho lần nào).
        [HttpGet("~/internal/products/variants")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVariantsInternal(CancellationToken ct)
        {
            var internalToken = config["Security:InternalApiKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
            if (!Request.Headers.TryGetValue("X-Internal-Key", out var headerKey) || headerKey != internalToken)
            {
                return Unauthorized("Yêu cầu này không hợp lệ hoặc thiếu mã khóa dịch vụ nội bộ.");
            }

            var result = await mediator.Send(new GetVariantsForSyncQuery(), ct);
            return Ok(result);
        }

        // Endpoint nội bộ cho service khác gọi sang (blog Service) để lấy nhanh nhiều sản phẩm
        // theo danh sách Id cùng lúc — dùng cho hiển thị "sản phẩm liên quan" trong bài blog.
        [HttpPost("~/internal/products/by-ids")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductsByIdsInternal([FromBody] List<Guid> productIds, CancellationToken ct)
        {
            var internalToken = config["Security:InternalApiKey"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Security:InternalApiKey");
            if (!Request.Headers.TryGetValue("X-Internal-Key", out var headerKey) || headerKey != internalToken)
            {
                return Unauthorized("Yêu cầu này không hợp lệ hoặc thiếu mã khóa dịch vụ nội bộ.");
            }

            var result = await mediator.Send(new GetProductsByIdsInternalQuery(productIds), ct);
            return Ok(result);
        }
    }
}
